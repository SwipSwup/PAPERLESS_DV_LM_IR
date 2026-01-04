using Core.DTOs;
using Core.DTOs.Messaging;

namespace OcrWorker.Storage
{
    public interface IStorageWrapper
    {
        public Task<string> DownloadPdfAsync(DocumentMessageDto message, CancellationToken ct = default);
        public Task UploadTextAsync(DocumentMessageDto message, string textContent, CancellationToken ct = default);
    }
}