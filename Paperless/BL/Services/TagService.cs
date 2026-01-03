using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BL.Services
{
    public class TagService
    {
        private readonly ILogger<TagService> _logger;
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository, ILogger<TagService> logger)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _logger = logger;
            _logger.LogInformation("TagService initialized");
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            _logger.LogInformation("TagService.GetAllTagsAsync called");
            try
            {
                return await _tagRepository.GetAllAsync();
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to retrieve all tags.", ex);
            }
        }

        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            _logger.LogInformation("TagService.GetTagByIdAsync called with ID={Id}", id);
            try
            {
                return await _tagRepository.GetByIdAsync(id);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to retrieve tag with ID {id}.", ex);
            }
        }

        public async Task<Tag> AddTagAsync(Tag tag)
        {
            _logger.LogInformation("TagService.AddTagAsync called for Tag Name='{TagName}'", tag.Name);
            try
            {
                await _tagRepository.AddAsync(tag);
                return tag;
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to add tag.", ex);
            }
        }

        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            _logger.LogInformation("TagService.UpdateTagAsync called for Tag ID={Id}", tag.Id);
            try
            {
                await _tagRepository.UpdateAsync(tag);
                return tag;
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to update tag with ID {tag.Id}.", ex);
            }
        }

        public async Task DeleteTagAsync(int id)
        {
            _logger.LogInformation("TagService.DeleteTagAsync called for ID={Id}", id);
            try
            {
                await _tagRepository.DeleteAsync(id);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to delete tag with ID {id}.", ex);
            }
        }

        public async Task<List<Tag>> SearchTagsAsync(string keyword)
        {
            _logger.LogInformation("TagService.SearchTagsAsync called with keyword='{Keyword}'", keyword);
            try
            {
                return await _tagRepository.SearchTagsAsync(keyword);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to search tags with keyword '{keyword}'.", ex);
            }
        }
    }
}
