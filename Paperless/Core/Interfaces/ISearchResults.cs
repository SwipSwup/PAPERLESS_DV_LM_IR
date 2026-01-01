using System.Collections.Generic;
using Core.DTOs;

namespace Core.Interfaces
{
    public interface ISearchResults
    {
        IEnumerable<DocumentDto> Results { get; }
        long TotalCount { get; }
    }
}
