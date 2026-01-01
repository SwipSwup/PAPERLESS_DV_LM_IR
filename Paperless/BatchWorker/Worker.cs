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

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("BatchWorker running at: {time}", DateTimeOffset.Now);

            try
            {
                var files = Directory.GetFiles(_inputPath, "*.xml");
                if (files.Length > 0)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();

                        foreach (var file in files)
                        {
                            await ProcessFileAsync(file, dbContext, stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch files");
            }

            // Quick polling for demo; in production use Crontab or longer delay
            await Task.Delay(10000, stoppingToken);
        }
    }

    private async Task ProcessFileAsync(string filePath, PaperlessDbContext db, CancellationToken ct)
    {
        _logger.LogInformation($"Processing file: {filePath}");
        try
        {
            var doc = XDocument.Load(filePath);
            var entries = doc.Descendants("Entry");

            foreach (var entry in entries)
            {
                var idStr = entry.Element("DocumentId")?.Value;
                var countStr = entry.Element("AccessCount")?.Value;

                if (int.TryParse(idStr, out int docId) && long.TryParse(countStr, out long count))
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
            }

            await db.SaveChangesAsync(ct);

            // Archive
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(_archivePath, $"{DateTime.Now:yyyyMMddHHmmss}_{fileName}");
            File.Move(filePath, destPath);
            _logger.LogInformation($"Archived {fileName} to {destPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process {filePath}");
            // Move to error folder? Or leave it? Leaving it might cause infinite loop. 
            // Better to move to archive with .err extension
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(_archivePath, $"{DateTime.Now:yyyyMMddHHmmss}_{fileName}.err");
            try { File.Move(filePath, destPath); } catch { } 
        }
    }
}