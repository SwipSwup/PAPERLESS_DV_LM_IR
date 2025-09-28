using AutoMapper;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;

namespace BL.Services
{
    public class AccessLogService(IAccessLogRepository accessLogRepo, IMapper mapper)
    {
        private readonly IAccessLogRepository _accessLogRepo = accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // --- CRUD Operations ---

        public async Task<List<AccessLogDto>> GetAllAsync()
        {
            List<AccessLog> logs = await _accessLogRepo.GetAllAsync();
            return _mapper.Map<List<AccessLogDto>>(logs);
        }

        public async Task<AccessLogDto?> GetByIdAsync(int id)
        {
            AccessLog? log = await _accessLogRepo.GetByIdAsync(id);
            return log == null ? null : _mapper.Map<AccessLogDto>(log);
        }

        public async Task<List<AccessLogDto>> GetByDocumentIdAsync(int documentId)
        {
            List<AccessLog> logs = await _accessLogRepo.GetByDocumentIdAsync(documentId);
            return _mapper.Map<List<AccessLogDto>>(logs);
        }

        public async Task<AccessLogDto> AddAsync(AccessLog log)
        {
            await _accessLogRepo.AddAsync(log);
            return _mapper.Map<AccessLogDto>(log);
        }

        public async Task<AccessLogDto> UpdateAsync(AccessLog log)
        {
            await _accessLogRepo.UpdateAsync(log);
            return _mapper.Map<AccessLogDto>(log);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            AccessLog? existing = await _accessLogRepo.GetByIdAsync(id);
            if (existing == null) 
                return false;

            await _accessLogRepo.DeleteAsync(id);
            return true;
        }

        // --- Business Logic ---

        public async Task LogAccessAsync(int documentId, DateTime date)
        {
            List<AccessLog> logs = await _accessLogRepo.GetByDocumentIdAsync(documentId);
            AccessLog? log = logs.FirstOrDefault(l => l.Date.Date == date.Date);

            if (log != null)
            {
                log.Count++;
                await _accessLogRepo.UpdateAsync(log);
            }
            else
            {
                await _accessLogRepo.AddAsync(new AccessLog
                {
                    Id = documentId,
                    Date = date,
                    Count = 1
                });
            }
        }
    }
}
