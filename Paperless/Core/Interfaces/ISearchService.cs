using System.Threading.Tasks;
using Core.DTOs;
using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Defines a contract for searching documents.
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Asynchronously searches for documents based on the provided search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (e.g., content, title, tags).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of matching documents.</returns>
        Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(string searchTerm);
    }
}