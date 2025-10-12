using AutoMapper;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class TagRepository(PaperlessDBContext context, IMapper mapper) : RepositoryBase, ITagRepository
{
    private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public Task<List<Tag>> GetAllAsync() =>
        ExecuteRepositoryActionAsync(async () =>
        {
            List<TagEntity> entities = await _context.Tags.Include(t => t.Documents).ToListAsync();
            return _mapper.Map<List<Tag>>(entities);
        }, "Failed to retrieve all Tags.");

    public Task<Tag?> GetByIdAsync(int id) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                TagEntity? entity = await _context.Tags.Include(t => t.Documents)
                    .FirstOrDefaultAsync(t => t.Id == id);
                return _mapper.Map<Tag?>(entity);
            }, $"Failed to retrieve Tag with ID {id}.");

    public Task AddAsync(Tag model) =>
        ExecuteRepositoryActionAsync(async () =>
        {
            TagEntity? entity = _mapper.Map<TagEntity>(model);
            await _context.Tags.AddAsync(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
        }, "Failed to add Tag.");

    public Task UpdateAsync(Tag model) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                TagEntity? entity = await _context.Tags.FindAsync(model.Id);
                if (entity == null)
                    throw new DataAccessException($"Tag {model.Id} not found.");

                _mapper.Map(model, entity);
                await _context.SaveChangesAsync();
            }, $"Failed to update Tag with ID {model.Id}.");

    public Task DeleteAsync(int id) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                TagEntity? entity = await _context.Tags.FindAsync(id);
                if (entity == null)
                    throw new DataAccessException($"Tag {id} not found.");

                _context.Tags.Remove(entity);
                await _context.SaveChangesAsync();
            }, $"Failed to delete Tag with ID {id}.");

    public Task<List<Tag>> SearchTagsAsync(string keyword) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                List<TagEntity> entities = await _context.Tags
                    .Where(t => t.Name.Contains(keyword))
                    .ToListAsync();
                return _mapper.Map<List<Tag>>(entities);
            }, $"Failed to search Tags with keyword '{keyword}'.");
}