using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class DocumentLogRepository : IRepository<DocumentLog>
{
    private readonly PaperlessDBContext _context;

    public DocumentLogRepository(PaperlessDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<DocumentLog>> GetAllAsync()
    {
        return await _context.DocumentLogs.Include(dl => dl.Document).ToListAsync();
    }

    public async Task<DocumentLog?> GetByIdAsync(int id)
    {
        return await _context.DocumentLogs.Include(dl => dl.Document)
            .FirstOrDefaultAsync(dl => dl.Id == id);
    }

    public async Task AddAsync(DocumentLog entity)
    {
        await _context.DocumentLogs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DocumentLog entity)
    {
        var existing = await _context.DocumentLogs.FindAsync(entity.Id);
        if (existing == null) throw new Exception($"DocumentLog {entity.Id} not found.");

        existing.Timestamp = entity.Timestamp;
        existing.Action = entity.Action;
        existing.Details = entity.Details;
        existing.DocumentId = entity.DocumentId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.DocumentLogs.FindAsync(id);
        if (entity != null)
        {
            _context.DocumentLogs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<DocumentLog>> GetByDocumentIdAsync(int documentId)
    {
        return await _context.DocumentLogs
            .Where(dl => dl.DocumentId == documentId)
            .ToListAsync();
    }
}