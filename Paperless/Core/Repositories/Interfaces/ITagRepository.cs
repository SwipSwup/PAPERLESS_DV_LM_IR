using Core.Models;

namespace Core.Repositories.Interfaces
{
    public interface ITagRepository : IRepository<Tag>
    {
        Task<List<Tag>> SearchTagsAsync(string keyword);
    }
}