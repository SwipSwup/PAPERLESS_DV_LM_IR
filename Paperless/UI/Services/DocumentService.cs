using System.Text.Json;
using Core.DTOs;

namespace UI.Services;

public class DocumentService : IDocumentService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync()
    {
        var response = await _httpClient.GetAsync("api/document");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DocumentDto>>(json, _jsonOptions) ?? new List<DocumentDto>();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/document/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DocumentDto>(json, _jsonOptions);
    }

    public async Task<DocumentDto> CreateDocumentAsync(DocumentDto document)
    {
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/document", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DocumentDto>(responseJson, _jsonOptions)!;
    }

    public async Task UpdateDocumentAsync(DocumentDto document)
    {
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/document/{document.Id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDocumentAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/document/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DocumentDto>> SearchDocumentsAsync(string keyword)
    {
        var response = await _httpClient.GetAsync($"api/document/search?keyword={Uri.EscapeDataString(keyword)}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DocumentDto>>(json, _jsonOptions) ?? new List<DocumentDto>();
    }
}

