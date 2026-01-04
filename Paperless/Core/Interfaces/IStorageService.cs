using System.IO;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    /// <summary>
    /// Defines a contract for file storage operations.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Asynchronously uploads a file to the storage system.
        /// </summary>
        /// <param name="fileStream">The stream of the file to upload.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the path or identifier of the uploaded file.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Asynchronously retrieves a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the file stream.</returns>
        Task<Stream> GetFileAsync(string filePath);

        /// <summary>
        /// Asynchronously deletes a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteFileAsync(string filePath);
    }
}