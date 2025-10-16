using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using log4net;
using System.Reflection;

namespace BL.Services
{
    public class TagService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            log.Info("TagService initialized");
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            log.Info("TagService.GetAllTagsAsync called");
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
            log.Info($"TagService.GetTagByIdAsync called with ID={id}");
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
            log.Info($"TagService.AddTagAsync called for Tag Name='{tag.Name}'");
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
            log.Info($"TagService.UpdateTagAsync called for Tag ID={tag.Id}");
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
            log.Info($"TagService.DeleteTagAsync called for ID={id}");
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
            log.Info($"TagService.SearchTagsAsync called with keyword='{keyword}'");
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
