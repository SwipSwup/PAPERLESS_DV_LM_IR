using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BL.Services
{
    public class AccessLogService(IAccessLogRepository accessLogRepo, IMapper mapper, ILogger<AccessLogService> logger)
    {
        private readonly ILogger<AccessLogService> _logger = logger;

        private readonly IAccessLogRepository _accessLogRepo =
            accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));

        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<List<AccessLogDto>> GetAllAsync()
        {
            _logger.LogInformation("AccessLogService.GetAllAsync called");
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
            _logger.LogInformation("AccessLogService.GetByIdAsync called with ID={Id}", id);
            try
            {
                AccessLog? logEntity = await _accessLogRepo.GetByIdAsync(id);
                return logEntity == null ? null : _mapper.Map<AccessLogDto>(logEntity);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to retrieve AccessLog with ID {id}.", ex);
            }
        }

        public async Task<List<AccessLogDto>> GetByDocumentIdAsync(int documentId)
        {
            _logger.LogInformation("AccessLogService.GetByDocumentIdAsync called for Document ID={Id}", documentId);
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

        public async Task<AccessLogDto> AddAsync(AccessLog accessLog)
        {
            _logger.LogInformation("AccessLogService.AddAsync called for Document ID={Id}", accessLog.Id);
            try
            {
                await _accessLogRepo.AddAsync(accessLog);
                return _mapper.Map<AccessLogDto>(accessLog);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to add AccessLog.", ex);
            }
        }

        public async Task<AccessLogDto> UpdateAsync(AccessLog accessLog)
        {
            _logger.LogInformation("AccessLogService.UpdateAsync called for AccessLog ID={Id}", accessLog.Id);
            try
            {
                await _accessLogRepo.UpdateAsync(accessLog);
                return _mapper.Map<AccessLogDto>(accessLog);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to update AccessLog with ID {accessLog.Id}.", ex);
            }
        }


        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("AccessLogService.DeleteAsync called for ID={Id}", id);
            try
            {
                AccessLog? existing = await _accessLogRepo.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("AccessLogService.DeleteAsync: AccessLog ID={Id} not found", id);
                    return false;
                }

                await _accessLogRepo.DeleteAsync(id);
                return true;
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to delete AccessLog with ID {id}.", ex);
            }
        }

        public async Task LogAccessAsync(int documentId, DateTime date)
        {
            _logger.LogInformation("AccessLogService.LogAccessAsync called for Document ID={Id} Date={Date}", documentId, date.ToString("yyyy-MM-dd"));
            try
            {
                List<AccessLog> logs = await _accessLogRepo.GetByDocumentIdAsync(documentId);
                AccessLog? logEntity = logs.FirstOrDefault(l => l.Date.Date == date.Date);

                if (logEntity != null)
                {
                    logEntity.Count++;
                    await _accessLogRepo.UpdateAsync(logEntity);
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
                throw new ServiceException($"Failed to log access for Document ID {documentId} on {date:yyyy-MM-dd}.", ex);
            }
        }
    }
}
