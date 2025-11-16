namespace Core.Repositories.Interfaces;

public interface IStorageRepository
{
    Task UploadFileAsync(string userId, string documentId, string fileName, Stream fileStream);
    Task<Stream?> GetFileAsync(string userId, string documentId, string fileName);
    Task DeleteFileAsync(string userId, string documentId, string fileName);
    Task<bool> FileExistsAsync(string userId, string documentId, string fileName);
}