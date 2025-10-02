using Core.DTOs;

namespace UI.Services;

public interface IDocumentService
{
    Task<List<DocumentDto>> GetAllDocumentsAsync();
    Task<DocumentDto?> GetDocumentByIdAsync(int id);
    Task<DocumentDto> CreateDocumentAsync(DocumentDto document);
    Task UpdateDocumentAsync(DocumentDto document);
    Task DeleteDocumentAsync(int id);
    Task<List<DocumentDto>> SearchDocumentsAsync(string keyword);
}

