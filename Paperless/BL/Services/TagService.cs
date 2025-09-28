using Core.Models;
using Core.Repositories.Interfaces;

namespace BL.Services;
public class TagService(ITagRepository tagRepository)
{
    private readonly ITagRepository _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

    // Get all tags
    public async Task<List<Tag>> GetAllTagsAsync()
    {
        return await _tagRepository.GetAllAsync();
    }

    // Get tag by Id
    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        return await _tagRepository.GetByIdAsync(id);
    }

    // Add a new tag
    public async Task<Tag> AddTagAsync(Tag tag)
    {
        await _tagRepository.AddAsync(tag);
        return tag;
    }

    // Update a tag
    public async Task<Tag> UpdateTagAsync(Tag tag)
    {
        await _tagRepository.UpdateAsync(tag);
        return tag;
    }

    // Delete a tag by Id
    public async Task DeleteTagAsync(int id)
    {
        await _tagRepository.DeleteAsync(id);
    }

    // Search tags by keyword
    public async Task<List<Tag>> SearchTagsAsync(string keyword)
    {
        return await _tagRepository.SearchTagsAsync(keyword);
    }
}