namespace OcrWorker.Utils;

public class TempFileUtility(ILogger<TempFileUtility> logger) : ITempFileUtility
{
    public string CreateTempDirectory(string? prefix = null)
    {
        string root = Path.GetTempPath();

        // /tmp/ocr_<prefix>_<guid> or /tmp/ocr_<guid>
        string folderName = prefix == null
            ? $"ocr_{Guid.NewGuid():N}"
            : $"ocr_{prefix}_{Guid.NewGuid():N}";

        string fullPath = Path.Combine(root, folderName);
        Directory.CreateDirectory(fullPath);

        logger.LogDebug("Created temp directory {path}", fullPath);
        return fullPath;
    }
    
    public string CreateTempFile(string directory, string extension)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string fileName = $"{Guid.NewGuid():N}.{extension}";
        string filePath = Path.Combine(directory, fileName);

        // Create empty file on disk
        using (File.Create(filePath)) { }

        logger.LogDebug("Created temp file {path}", filePath);
        return filePath;
    }
    
    public async Task DeleteDirectoryAsync(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return;

            await Task.Run(() => Directory.Delete(directory, recursive: true));

            logger.LogDebug("Deleted temp directory {path}", directory);
        }
        catch (Exception ex)
        {
            // Cleanup helper should never crash the worker
            logger.LogWarning(ex, "Failed to delete temp directory {path}", directory);
        }
    }
}
