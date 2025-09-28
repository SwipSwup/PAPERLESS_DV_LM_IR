using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class TagRepository : IRepository<Tag>
{
    private readonly PaperlessDBContext _context;

    public TagRepository(PaperlessDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<Tag>> GetAllAsync()
    {
        return await _context.Tags.Include(t => t.Documents).ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags.Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(Tag entity)
    {
        await _context.Tags.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tag entity)
    {
        var existing = await _context.Tags.FindAsync(entity.Id);
        if (existing == null) throw new Exception($"Tag {entity.Id} not found.");

        existing.Name = entity.Name;
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
        return await _context.Tags
            .Where(t => t.Name.Contains(keyword))
            .ToListAsync();
    }
}