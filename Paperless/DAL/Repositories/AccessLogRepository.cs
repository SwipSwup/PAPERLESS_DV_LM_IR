using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class AccessLogRepository : IRepository<AccessLog>
{
    private readonly PaperlessDBContext _context;

    public AccessLogRepository(PaperlessDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<AccessLog>> GetAllAsync()
    {
        return await _context.AccessLogs.Include(a => a.Document).ToListAsync();
    }

    public async Task<AccessLog?> GetByIdAsync(int id)
    {
        return await _context.AccessLogs.Include(a => a.Document)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddAsync(AccessLog entity)
    {
        await _context.AccessLogs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AccessLog entity)
    {
        var existing = await _context.AccessLogs.FindAsync(entity.Id);
        if (existing == null) throw new Exception($"AccessLog {entity.Id} not found.");

        existing.Date = entity.Date;
        existing.Count = entity.Count;
        existing.DocumentId = entity.DocumentId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AccessLogs.FindAsync(id);
        if (entity != null)
        {
            _context.AccessLogs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<AccessLog>> GetByDocumentIdAsync(int documentId)
    {
        return await _context.AccessLogs
            .Where(a => a.DocumentId == documentId)
            .ToListAsync();
    }
}