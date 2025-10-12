using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;

namespace BL.Services
{
    public class AccessLogService(IAccessLogRepository accessLogRepo, IMapper mapper)
    {
        private readonly IAccessLogRepository _accessLogRepo =
            accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));

        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // --- CRUD Operations ---
        public async Task<List<AccessLogDto>> GetAllAsync()
        {
            try
            {
                List<AccessLog> logs = await _accessLogRepo.GetAllAsync();
                return _mapper.Map<List<AccessLogDto>>(logs);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to retrieve all AccessLogs.", ex);
            }
        }

        public async Task<AccessLogDto?> GetByIdAsync(int id)
        {
            try
            {
                AccessLog? log = await _accessLogRepo.GetByIdAsync(id);
                return log == null ? null : _mapper.Map<AccessLogDto>(log);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to retrieve AccessLog with ID {id}.", ex);
            }
        }

        public async Task<List<AccessLogDto>> GetByDocumentIdAsync(int documentId)
        {
            try
            {
                List<AccessLog> logs = await _accessLogRepo.GetByDocumentIdAsync(documentId);
                return _mapper.Map<List<AccessLogDto>>(logs);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to retrieve AccessLogs for Document ID {documentId}.", ex);
            }
        }

        public async Task<AccessLogDto> AddAsync(AccessLog log)
        {
            try
            {
                await _accessLogRepo.AddAsync(log);
                return _mapper.Map<AccessLogDto>(log);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to add AccessLog.", ex);
            }
        }

        public async Task<AccessLogDto> UpdateAsync(AccessLog log)
        {
            try
            {
                await _accessLogRepo.UpdateAsync(log);
                return _mapper.Map<AccessLogDto>(log);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to update AccessLog with ID {log.Id}.", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                AccessLog? existing = await _accessLogRepo.GetByIdAsync(id);
                if (existing == null) return false;

                await _accessLogRepo.DeleteAsync(id);
                return true;
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to delete AccessLog with ID {id}.", ex);
            }
        }

        // --- Business Logic ---

        public async Task LogAccessAsync(int documentId, DateTime date)
        {
            try
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
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to log access for Document ID {documentId} on {date:yyyy-MM-dd}.",
                    ex);
            }
        }
    }
}