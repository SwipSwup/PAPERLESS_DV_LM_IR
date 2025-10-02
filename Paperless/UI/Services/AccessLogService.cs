using System.Text.Json;
using Core.DTOs;

namespace UI.Services;

public class AccessLogService : IAccessLogService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AccessLogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<AccessLogDto>> GetAllAccessLogsAsync()
    {
        var response = await _httpClient.GetAsync("api/accesslog");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AccessLogDto>>(json, _jsonOptions) ?? new List<AccessLogDto>();
    }

    public async Task<AccessLogDto?> GetAccessLogByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/accesslog/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AccessLogDto>(json, _jsonOptions);
    }
}

