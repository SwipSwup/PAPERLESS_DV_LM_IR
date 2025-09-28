
using Core.Models;

namespace Core.Repositories.Interfaces
{
    public interface IAccessLogRepository : IRepository<AccessLog>
    {
        Task<List<AccessLog>> GetByDocumentIdAsync(int documentId);
    }
}