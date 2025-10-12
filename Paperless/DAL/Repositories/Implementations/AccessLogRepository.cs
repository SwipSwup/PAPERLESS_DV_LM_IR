using DAL.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;

namespace DAL.Repositories.Implementations
{
    public class AccessLogRepository(PaperlessDBContext context, IMapper mapper) : RepositoryBase, IAccessLogRepository
    {
        private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public Task<List<AccessLog>> GetAllAsync() =>
            ExecuteRepositoryActionAsync(async () =>
            {
                List<AccessLogEntity> entities = await _context.AccessLogs.Include(a => a.DocumentEntity).ToListAsync();
                return _mapper.Map<List<AccessLog>>(entities);
            }, "Failed to retrieve all AccessLogs.");

        public Task<AccessLog?> GetByIdAsync(int id) =>
            ExecuteRepositoryActionAsync(async () =>
                {
                    AccessLogEntity? entity = await _context.AccessLogs
                        .Include(a => a.DocumentEntity)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    return _mapper.Map<AccessLog?>(entity);
                }, $"Failed to retrieve AccessLog with ID {id}.");

        public Task AddAsync(AccessLog model) =>
            ExecuteRepositoryActionAsync(async () =>
            {
                AccessLogEntity? entity = _mapper.Map<AccessLogEntity>(model);
                await _context.AccessLogs.AddAsync(entity);
                await _context.SaveChangesAsync();
                model.Id = entity.Id;
            }, "Failed to add AccessLog.");

        public Task UpdateAsync(AccessLog model) =>
            ExecuteRepositoryActionAsync(async () =>
                {
                    AccessLogEntity? entity = await _context.AccessLogs.FindAsync(model.Id);
                    if (entity == null)
                        throw new DataAccessException($"AccessLog {model.Id} not found.");

                    _mapper.Map(model, entity);
                    await _context.SaveChangesAsync();
                }, $"Failed to update AccessLog with ID {model.Id}.");

        public Task DeleteAsync(int id) =>
            ExecuteRepositoryActionAsync(async () =>
                {
                    AccessLogEntity? entity = await _context.AccessLogs.FindAsync(id);
                    if (entity == null)
                        throw new DataAccessException($"AccessLog {id} not found.");

                    _context.AccessLogs.Remove(entity);
                    await _context.SaveChangesAsync();
                }, $"Failed to delete AccessLog with ID {id}.");

        public Task<List<AccessLog>> GetByDocumentIdAsync(int documentId) =>
            ExecuteRepositoryActionAsync(async () =>
                {
                    List<AccessLogEntity> entities = await _context.AccessLogs
                        .Where(a => a.DocumentId == documentId)
                        .ToListAsync();

                    return _mapper.Map<List<AccessLog>>(entities);
                }, $"Failed to retrieve AccessLogs for Document ID {documentId}.");
    }
}