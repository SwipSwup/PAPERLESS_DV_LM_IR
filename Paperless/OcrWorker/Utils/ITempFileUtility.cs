namespace OcrWorker.Utils
{
    public interface ITempFileUtility
    {
        public string CreateTempDirectory(string? prefix = null);

        public string CreateTempFile(string directory, string extension);

        public Task DeleteDirectoryAsync(string directory);
    }
}