using Core.Models;

namespace Core.Repositories.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<List<Document>> SearchDocumentsAsync(string keyword);
        Task<List<AccessLog>> GetAccessLogsForDocumentAsync(int documentId);
        Task<List<DocumentLog>> GetLogsForDocumentAsync(int documentId);
    }
}