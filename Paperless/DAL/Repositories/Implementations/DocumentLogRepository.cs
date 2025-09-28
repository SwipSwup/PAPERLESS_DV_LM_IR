using AutoMapper;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public class DocumentLogRepository(PaperlessDBContext context, IMapper mapper) : IDocumentLogRepository
{
    private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<List<DocumentLog>> GetAllAsync()
    {
        var entities = await _context.DocumentLogs.Include(dl => dl.DocumentEntity).ToListAsync();
        return _mapper.Map<List<DocumentLog>>(entities);
    }

    public async Task<DocumentLog?> GetByIdAsync(int id)
    {
        var entity = await _context.DocumentLogs.Include(dl => dl.DocumentEntity)
            .FirstOrDefaultAsync(dl => dl.Id == id);

        return _mapper.Map<DocumentLog?>(entity);
    }

    public async Task AddAsync(DocumentLog model)
    {
        var entity = _mapper.Map<DocumentLogEntity>(model);
        await _context.DocumentLogs.AddAsync(entity);
        await _context.SaveChangesAsync();
        model.Id = entity.Id;
    }

    public async Task UpdateAsync(DocumentLog model)
    {
        var entity = await _context.DocumentLogs.FindAsync(model.Id);
        if (entity == null) throw new Exception($"DocumentLog {model.Id} not found.");

        _mapper.Map(model, entity);
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
        var entities = await _context.DocumentLogs
            .Where(dl => dl.DocumentId == documentId)
            .ToListAsync();

        return _mapper.Map<List<DocumentLog>>(entities);
    }
}
