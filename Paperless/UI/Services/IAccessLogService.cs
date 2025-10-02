using Core.DTOs;

namespace UI.Services;

public interface IAccessLogService
{
    Task<List<AccessLogDto>> GetAllAccessLogsAsync();
    Task<AccessLogDto?> GetAccessLogByIdAsync(int id);
}

