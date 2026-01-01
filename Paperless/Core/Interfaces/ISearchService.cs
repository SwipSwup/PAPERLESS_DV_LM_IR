using System.Threading.Tasks;
using Core.DTOs;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(string searchTerm);
    }
}
