using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class DocumentRepository : IRepository<Document>
{
    private readonly PaperlessDBContext _context;

    public DocumentRepository(PaperlessDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<Document>> GetAllAsync()
    {
        return await _context.Documents
            .Include(d => d.Tags)
            .Include(d => d.AccessLogs)
            .Include(d => d.Logs)
            .ToListAsync();
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.Tags)
            .Include(d => d.AccessLogs)
            .Include(d => d.Logs)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(Document entity)
    {
        await _context.Documents.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document entity)
    {
        var existing = await _context.Documents.FindAsync(entity.Id);
        if (existing == null) throw new Exception($"Document {entity.Id} not found.");

        existing.FileName = entity.FileName;
        existing.FilePath = entity.FilePath;
        existing.OcrText = entity.OcrText;
        existing.Summary = entity.Summary;
        existing.UploadedAt = entity.UploadedAt;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Documents.FindAsync(id);
        if (entity != null)
        {
            _context.Documents.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    // Extra methods
    public async Task<List<Document>> SearchDocumentsAsync(string keyword)
    {
        return await _context.Documents
            .Include(d => d.Tags)
            .Where(d => d.FileName.Contains(keyword) ||
                        (d.Summary != null && d.Summary.Contains(keyword)) ||
                        (d.OcrText != null && d.OcrText.Contains(keyword)))
            .ToListAsync();
    }

    public async Task<List<AccessLog>> GetAccessLogsForDocumentAsync(int documentId)
    {
        return await _context.AccessLogs
            .Where(a => a.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task<List<DocumentLog>> GetLogsForDocumentAsync(int documentId)
    {
        return await _context.DocumentLogs
            .Where(dl => dl.DocumentId == documentId)
            .ToListAsync();
    }
}
