using DAL.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Core.Models;
using Core.Repositories.Interfaces;

namespace DAL.Repositories.Implementations
{
    public class AccessLogRepository(PaperlessDBContext context, IMapper mapper) : IAccessLogRepository
    {
        private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<List<AccessLog>> GetAllAsync()
        {
            var entities = await _context.AccessLogs.Include(a => a.DocumentEntity).ToListAsync();
            return _mapper.Map<List<AccessLog>>(entities);
        }

        public async Task<AccessLog?> GetByIdAsync(int id)
        {
            var entity = await _context.AccessLogs
                .Include(a => a.DocumentEntity)
                .FirstOrDefaultAsync(a => a.Id == id);

            return _mapper.Map<AccessLog?>(entity);
        }

        public async Task AddAsync(AccessLog model)
        {
            var entity = _mapper.Map<AccessLogEntity>(model);
            await _context.AccessLogs.AddAsync(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
        }

        public async Task UpdateAsync(AccessLog model)
        {
            var entity = await _context.AccessLogs.FindAsync(model.Id);
            if (entity == null) throw new Exception($"AccessLog {model.Id} not found.");

            _mapper.Map(model, entity);

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
            var entities = await _context.AccessLogs
                .Where(a => a.DocumentId == documentId)
                .ToListAsync();

            return _mapper.Map<List<AccessLog>>(entities);
        }
    }
}
