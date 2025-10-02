using Core.DTOs;

namespace UI.Services;

public interface ITagService
{
    Task<List<TagDto>> GetAllTagsAsync();
    Task<TagDto?> GetTagByIdAsync(int id);
    Task<TagDto> CreateTagAsync(TagDto tag);
    Task UpdateTagAsync(TagDto tag);
    Task DeleteTagAsync(int id);
    Task<List<TagDto>> SearchTagsAsync(string keyword);
}

