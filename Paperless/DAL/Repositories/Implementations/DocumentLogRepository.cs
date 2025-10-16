using AutoMapper;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using log4net;
using System.Reflection;

namespace DAL.Repositories.Implementations
{
    public class DocumentLogRepository(PaperlessDBContext context, IMapper mapper) : RepositoryBase, IDocumentLogRepository
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public Task<List<DocumentLog>> GetAllAsync()
        {
            log.Info("DocumentLogRepository.GetAllAsync called");
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentLogEntity> entities = await _context.DocumentLogs.Include(dl => dl.DocumentEntity).ToListAsync();
                return _mapper.Map<List<DocumentLog>>(entities);
            }, "Failed to retrieve all DocumentLogs.");
        }

        public Task<DocumentLog?> GetByIdAsync(int id)
        {
            log.Info($"DocumentLogRepository.GetByIdAsync called for ID={id}");
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentLogEntity? entity = await _context.DocumentLogs.Include(dl => dl.DocumentEntity)
                    .FirstOrDefaultAsync(dl => dl.Id == id);
                return _mapper.Map<DocumentLog?>(entity);
            }, $"Failed to retrieve DocumentLog with ID {id}.");
        }

        public Task AddAsync(DocumentLog model)
        {
            log.Info($"DocumentLogRepository.AddAsync called for Document ID={model.Id}");
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentLogEntity? entity = _mapper.Map<DocumentLogEntity>(model);
                await _context.DocumentLogs.AddAsync(entity);
                await _context.SaveChangesAsync();
                model.Id = entity.Id;
            }, "Failed to add DocumentLog.");
        }

        public Task UpdateAsync(DocumentLog model)
        {
            log.Info($"DocumentLogRepository.UpdateAsync called for DocumentLog ID={model.Id}");
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentLogEntity? entity = await _context.DocumentLogs.FindAsync(model.Id);
                if (entity == null)
                    throw new DataAccessException($"DocumentLog {model.Id} not found.");

                _mapper.Map(model, entity);
                await _context.SaveChangesAsync();
            }, $"Failed to update DocumentLog with ID {model.Id}.");
        }

        public Task DeleteAsync(int id)
        {
            log.Info($"DocumentLogRepository.DeleteAsync called for DocumentLog ID={id}");
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentLogEntity? entity = await _context.DocumentLogs.FindAsync(id);
                if (entity == null)
                    throw new DataAccessException($"DocumentLog {id} not found.");

                _context.DocumentLogs.Remove(entity);
                await _context.SaveChangesAsync();
            }, $"Failed to delete DocumentLog with ID {id}.");
        }

        public Task<List<DocumentLog>> GetByDocumentIdAsync(int documentId)
        {
            log.Info($"DocumentLogRepository.GetByDocumentIdAsync called for Document ID={documentId}");
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentLogEntity> entities = await _context.DocumentLogs
                    .Where(dl => dl.DocumentId == documentId)
                    .ToListAsync();
                return _mapper.Map<List<DocumentLog>>(entities);
            }, $"Failed to retrieve DocumentLogs for Document ID {documentId}.");
        }
    }
}
