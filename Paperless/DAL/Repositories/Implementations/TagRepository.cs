using AutoMapper;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class TagRepository(PaperlessDBContext context, IMapper mapper) : ITagRepository
{
    private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<List<Tag>> GetAllAsync()
    {
        var entities = await _context.Tags.Include(t => t.Documents).ToListAsync();
        return _mapper.Map<List<Tag>>(entities);
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        var entity = await _context.Tags.Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == id);

        return _mapper.Map<Tag?>(entity);
    }

    public async Task AddAsync(Tag model)
    {
        var entity = _mapper.Map<TagEntity>(model);
        await _context.Tags.AddAsync(entity);
        await _context.SaveChangesAsync();
        model.Id = entity.Id;
    }

    public async Task UpdateAsync(Tag model)
    {
        var entity = await _context.Tags.FindAsync(model.Id);
        if (entity == null) throw new Exception($"Tag {model.Id} not found.");

        _mapper.Map(model, entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Tags.FindAsync(id);
        if (entity != null)
        {
            _context.Tags.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Tag>> SearchTagsAsync(string keyword)
    {
        var entities = await _context.Tags
            .Where(t => t.Name.Contains(keyword))
            .ToListAsync();

        return _mapper.Map<List<Tag>>(entities);
    }
}