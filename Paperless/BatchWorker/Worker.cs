using System.Xml.Linq;
using DAL;
using DAL.Models;

namespace BatchWorker;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    : BackgroundService
{
    private readonly string _inputPath = configuration["BatchSettings:InputPath"] ?? "/app/import";
    private readonly string _archivePath = configuration["BatchSettings:ArchivePath"] ?? "/app/archive";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure directories exist
        Directory.CreateDirectory(_inputPath);
        Directory.CreateDirectory(_archivePath);

        logger.LogInformation("BatchWorker started. Watching {input} and archiving to {archive}", _inputPath, _archivePath);

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
            logger.LogInformation("Next batch run scheduled for {nextRun} (in {delay})", nextRun, delay);

            try
            {
                // Wait until schedule or cancellation
                await Task.Delay(delay, stoppingToken);

                // Check again in case of spurious wakeups or massive drift, though Delay is usually good.
                // Doing the work:
                string[] files = Directory.GetFiles(_inputPath, "*.xml");
                if (files.Length > 0)
                {
                    logger.LogInformation("Starting scheduled batch processing. Found {count} files.", files.Length);

                    using IServiceScope scope = serviceProvider.CreateScope();
                    PaperlessDBContext dbContext = scope.ServiceProvider.GetRequiredService<PaperlessDBContext>();

                    foreach (string file in files)
                    {
                        await ProcessFileAsync(file, dbContext, stoppingToken);
                    }
                }
                else
                {
                    logger.LogInformation("No files found to process at scheduled time.");
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing batch files");
            }
        }
    }

    private async Task ProcessFileAsync(string filePath, PaperlessDBContext db, CancellationToken ct)
    {
        logger.LogInformation($"Processing file: {filePath}");
        bool success = false;
        try
        {
            XDocument doc = XDocument.Load(filePath);
            IEnumerable<XElement> entries = doc.Descendants("Entry");

            if (!entries.Any())
            {
                logger.LogWarning("File {file} contains no Entry elements", filePath);
            }

            foreach (XElement entry in entries)
            {
                XElement? idElement = entry.Element("DocumentId");
                XElement? countElement = entry.Element("AccessCount");

                if (idElement == null || countElement == null)
                {
                    logger.LogWarning("Skipping invalid entry in {file}: Missing DocumentId or AccessCount", filePath);
                    continue;
                }

                if (int.TryParse(idElement.Value, out int docId) && long.TryParse(countElement.Value, out long count))
                {
                    DocumentEntity? document = await db.Documents.FindAsync([docId], ct);
                    if (document != null)
                    {
                        document.AccessCount += count;
                        logger.LogInformation($"Updated Doc {docId}: +{count} views. New total: {document.AccessCount}");
                    }
                    else
                    {
                        logger.LogWarning($"Document {docId} not found.");
                    }
                }
                else
                {
                    logger.LogWarning("Skipping invalid entry in {file}: Check data types", filePath);
                }
            }

            await db.SaveChangesAsync(ct);
            success = true;
        }
        catch (System.Xml.XmlException ex)
        {
            logger.LogError(ex, "Invalid XML format in file {file}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {file}", filePath);
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
            string fileName = Path.GetFileName(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string destinationName = $"{timestamp}_{uniqueId}_{fileName}";
            
            if (!success)
            {
                destinationName += ".err";
            }

            string destPath = Path.Combine(_archivePath, destinationName);
            
            // Ensure unique path (extra safety)
            if (File.Exists(destPath))
            {
                 destPath = Path.Combine(_archivePath, $"{timestamp}_{Guid.NewGuid()}_{fileName}" + (!success ? ".err" : ""));
            }

            File.Move(filePath, destPath);
            logger.LogInformation(success ? "Archived {file} to {dest}" : "Moved failed file {file} to {dest}", fileName, destPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Failed to archive file {file}. Code may loop if not handled.", filePath);
            
            // Last resort: try to rename in place if archive fails, to avoid infinite processing loop
            try 
            {
                 string errorPath = filePath + ".error";
                 if (File.Exists(errorPath)) File.Delete(errorPath);
                 File.Move(filePath, errorPath);
            } 
            catch { /* Give up */ }
        }
    }
}
