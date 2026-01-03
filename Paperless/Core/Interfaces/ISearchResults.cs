using System.Collections.Generic;
using Core.DTOs;

namespace Core.Interfaces
{
    /// <summary>
    /// Represents the results of a search operation.
    /// </summary>
    public interface ISearchResults
    {
        /// <summary>
        /// Gets the collection of documents matching the search criteria.
        /// </summary>
        IEnumerable<DocumentDto> Results { get; }

        /// <summary>
        /// Gets the total count of documents found.
        /// </summary>
        long TotalCount { get; }
    }
}
