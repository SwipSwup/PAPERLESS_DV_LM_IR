using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class DocumentRepository(PaperlessDBContext context, IMapper mapper) : RepositoryBase, IDocumentRepository
{
    private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public Task<List<Document>> GetAllAsync() =>
        ExecuteRepositoryActionAsync(async () =>
        {
            List<DocumentEntity> entities = await _context.Documents
                .Include(d => d.Tags)
                .Include(d => d.AccessLogs)
                .Include(d => d.Logs)
                .ToListAsync();
            return _mapper.Map<List<Document>>(entities);
        }, "Failed to retrieve all Documents.");

    public Task<Document?> GetByIdAsync(int id) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents
                    .Include(d => d.Tags)
                    .Include(d => d.AccessLogs)
                    .Include(d => d.Logs)
                    .FirstOrDefaultAsync(d => d.Id == id);
                return _mapper.Map<Document?>(entity);
            }, $"Failed to retrieve Document with ID {id}.");

    public Task AddAsync(Document model) =>
        ExecuteRepositoryActionAsync(async () =>
        {
            DocumentEntity? entity = _mapper.Map<DocumentEntity>(model);
            await _context.Documents.AddAsync(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
        }, "Failed to add Document.");

    public Task UpdateAsync(Document model) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents.FindAsync(model.Id);
                if (entity == null)
                    throw new DataAccessException($"Document {model.Id} not found.");

                _mapper.Map(model, entity);
                await _context.SaveChangesAsync();
            }, $"Failed to update Document with ID {model.Id}.");

    public Task DeleteAsync(int id) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents.FindAsync(id);
                if (entity == null)
                    throw new DataAccessException($"Document {id} not found.");

                _context.Documents.Remove(entity);
                await _context.SaveChangesAsync();
            }, $"Failed to delete Document with ID {id}.");

    // Extra methods
    public Task<List<Document>> SearchDocumentsAsync(string keyword) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentEntity> entities = await _context.Documents
                    .Include(d => d.Tags)
                    .Where(d => d.FileName.Contains(keyword) ||
                                (d.Summary != null && d.Summary.Contains(keyword)) ||
                                (d.OcrText != null && d.OcrText.Contains(keyword)))
                    .ToListAsync();
                return _mapper.Map<List<Document>>(entities);
            }, $"Failed to search Documents with keyword '{keyword}'.");

    public Task<List<AccessLog>> GetAccessLogsForDocumentAsync(int documentId) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                List<AccessLogEntity> entities = await _context.AccessLogs
                    .Where(a => a.DocumentId == documentId)
                    .ToListAsync();
                return _mapper.Map<List<AccessLog>>(entities);
            }, $"Failed to retrieve AccessLogs for Document ID {documentId}.");

    public Task<List<DocumentLog>> GetLogsForDocumentAsync(int documentId) =>
        ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentLogEntity> entities = await _context.DocumentLogs
                    .Where(dl => dl.DocumentId == documentId)
                    .ToListAsync();
                return _mapper.Map<List<DocumentLog>>(entities);
            }, $"Failed to retrieve DocumentLogs for Document ID {documentId}.");
}