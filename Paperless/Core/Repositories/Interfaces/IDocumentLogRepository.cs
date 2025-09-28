using Core.Models;

namespace Core.Repositories.Interfaces
{
    public interface IDocumentLogRepository : IRepository<DocumentLog>
    {
        Task<List<DocumentLog>> GetByDocumentIdAsync(int documentId);
    }
}