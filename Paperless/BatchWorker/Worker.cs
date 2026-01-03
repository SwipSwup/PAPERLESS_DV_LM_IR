using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Paperless.DAL;

namespace Paperless.BatchWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _inputPath;
    private readonly string _archivePath;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _inputPath = configuration["BatchSettings:InputPath"] ?? "/app/import";
        _archivePath = configuration["BatchSettings:ArchivePath"] ?? "/app/archive";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure directories exist
        Directory.CreateDirectory(_inputPath);
        Directory.CreateDirectory(_archivePath);

        _logger.LogInformation("BatchWorker started. Watching {input} and archiving to {archive}", _inputPath, _archivePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate delay until next 01:00 AM
            DateTime now = DateTime.Now;
            DateTime nextRun = now.Date.AddHours(1); // 01:00 AM today
            if (now >= nextRun)
            {
                nextRun = nextRun.AddDays(1); // 01:00 AM tomorrow
            }

            TimeSpan delay = nextRun - now;
            _logger.LogInformation("Next batch run scheduled for {nextRun} (in {delay})", nextRun, delay);

            try
            {
                // Wait until schedule or cancellation
                await Task.Delay(delay, stoppingToken);

                // Check again in case of spurious wakeups or massive drift, though Delay is usually good.
                // Doing the work:
                var files = Directory.GetFiles(_inputPath, "*.xml");
                if (files.Length > 0)
                {
                    _logger.LogInformation("Starting scheduled batch processing. Found {count} files.", files.Length);
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();

                        foreach (var file in files)
                        {
                            await ProcessFileAsync(file, dbContext, stoppingToken);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No files found to process at scheduled time.");
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch files");
            }
        }
    }

    private async Task ProcessFileAsync(string filePath, PaperlessDbContext db, CancellationToken ct)
    {
        _logger.LogInformation($"Processing file: {filePath}");
        bool success = false;
        try
        {
            var doc = XDocument.Load(filePath);
            var entries = doc.Descendants("Entry");

            if (!entries.Any())
            {
                _logger.LogWarning("File {file} contains no Entry elements", filePath);
            }

            foreach (var entry in entries)
            {
                var idElement = entry.Element("DocumentId");
                var countElement = entry.Element("AccessCount");

                if (idElement == null || countElement == null)
                {
                    _logger.LogWarning("Skipping invalid entry in {file}: Missing DocumentId or AccessCount", filePath);
                    continue;
                }

                if (int.TryParse(idElement.Value, out int docId) && long.TryParse(countElement.Value, out long count))
                {
                    var document = await db.Documents.FindAsync(new object[] { docId }, ct);
                    if (document != null)
                    {
                        document.AccessCount += count;
                        _logger.LogInformation($"Updated Doc {docId}: +{count} views. New total: {document.AccessCount}");
                    }
                    else
                    {
                        _logger.LogWarning($"Document {docId} not found.");
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping invalid entry in {file}: Check data types", filePath);
                }
            }

            await db.SaveChangesAsync(ct);
            success = true;
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogError(ex, "Invalid XML format in file {file}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {file}", filePath);
        }
        finally
        {
            // Archive the file regardless of success (maybe with .err suffix if failed)
            ArchiveFile(filePath, success);
        }
    }

    private void ArchiveFile(string filePath, bool success)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var destinationName = $"{timestamp}_{uniqueId}_{fileName}";
            
            if (!success)
            {
                destinationName += ".err";
            }

            var destPath = Path.Combine(_archivePath, destinationName);
            
            // Ensure unique path (extra safety)
            if (File.Exists(destPath))
            {
                 destPath = Path.Combine(_archivePath, $"{timestamp}_{Guid.NewGuid()}_{fileName}" + (!success ? ".err" : ""));
            }

            File.Move(filePath, destPath);
            _logger.LogInformation(success ? "Archived {file} to {dest}" : "Moved failed file {file} to {dest}", fileName, destPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Failed to archive file {file}. Code may loop if not handled.", filePath);
            
            // Last resort: try to rename in place if archive fails, to avoid infinite processing loop
            try 
            {
                 var errorPath = filePath + ".error";
                 if (File.Exists(errorPath)) File.Delete(errorPath);
                 File.Move(filePath, errorPath);
            } 
            catch { /* Give up */ }
        }
    }
}
