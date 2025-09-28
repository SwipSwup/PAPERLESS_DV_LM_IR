using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class DocumentRepository(PaperlessDBContext context, IMapper mapper) : IDocumentRepository
{
    private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<List<Document>> GetAllAsync()
    {
        var entities = await _context.Documents
            .Include(d => d.Tags)
            .Include(d => d.AccessLogs)
            .Include(d => d.Logs)
            .ToListAsync();

        return _mapper.Map<List<Document>>(entities);
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        var entity = await _context.Documents
            .Include(d => d.Tags)
            .Include(d => d.AccessLogs)
            .Include(d => d.Logs)
            .FirstOrDefaultAsync(d => d.Id == id);

        return _mapper.Map<Document?>(entity);
    }

    public async Task AddAsync(Document model)
    {
        var entity = _mapper.Map<DocumentEntity>(model);
        await _context.Documents.AddAsync(entity);
        await _context.SaveChangesAsync();
        model.Id = entity.Id; // sync back
    }

    public async Task UpdateAsync(Document model)
    {
        var entity = await _context.Documents.FindAsync(model.Id);
        if (entity == null) throw new Exception($"Document {model.Id} not found.");

        _mapper.Map(model, entity);
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
        var entities = await _context.Documents
            .Include(d => d.Tags)
            .Where(d => d.FileName.Contains(keyword) ||
                        (d.Summary != null && d.Summary.Contains(keyword)) ||
                        (d.OcrText != null && d.OcrText.Contains(keyword)))
            .ToListAsync();

        return _mapper.Map<List<Document>>(entities);
    }

    public async Task<List<AccessLog>> GetAccessLogsForDocumentAsync(int documentId)
    {
        var entities = await _context.AccessLogs
            .Where(a => a.DocumentId == documentId)
            .ToListAsync();

        return _mapper.Map<List<AccessLog>>(entities);
    }

    public async Task<List<DocumentLog>> GetLogsForDocumentAsync(int documentId)
    {
        var entities = await _context.DocumentLogs
            .Where(dl => dl.DocumentId == documentId)
            .ToListAsync();

        return _mapper.Map<List<DocumentLog>>(entities);
    }
}
