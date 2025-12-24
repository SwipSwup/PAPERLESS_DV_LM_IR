using System.IO;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }
}
