using System.Text.Json;
using Core.DTOs;

namespace UI.Services;

public class TagService : ITagService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public TagService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<TagDto>> GetAllTagsAsync()
    {
        var response = await _httpClient.GetAsync("api/tag");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TagDto>>(json, _jsonOptions) ?? new List<TagDto>();
    }

    public async Task<TagDto?> GetTagByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/tag/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TagDto>(json, _jsonOptions);
    }

    public async Task<TagDto> CreateTagAsync(TagDto tag)
    {
        var json = JsonSerializer.Serialize(tag, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/tag", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TagDto>(responseJson, _jsonOptions)!;
    }

    public async Task UpdateTagAsync(TagDto tag)
    {
        var json = JsonSerializer.Serialize(tag, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/tag/{tag.Id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTagAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/tag/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<TagDto>> SearchTagsAsync(string keyword)
    {
        var response = await _httpClient.GetAsync($"api/tag/search?keyword={Uri.EscapeDataString(keyword)}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TagDto>>(json, _jsonOptions) ?? new List<TagDto>();
    }
}

