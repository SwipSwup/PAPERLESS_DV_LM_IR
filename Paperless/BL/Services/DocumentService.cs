using AutoMapper;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;

namespace BL.Services
{
    public class DocumentService(
        IDocumentRepository documentRepo,
        IAccessLogRepository accessLogRepo,
        IDocumentLogRepository documentLogRepo,
        IMapper mapper)
    {
        private readonly IDocumentRepository _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        private readonly IAccessLogRepository _accessLogRepo = accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));
        private readonly IDocumentLogRepository _documentLogRepo = documentLogRepo ?? throw new ArgumentNullException(nameof(documentLogRepo));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // --- CRUD Operations ---
        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            List<Document> documents = await _documentRepo.GetAllAsync();
            return _mapper.Map<List<DocumentDto>>(documents);
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            Document? document = await _documentRepo.GetByIdAsync(id);
            return document == null ? null : _mapper.Map<DocumentDto>(document);
        }

        public async Task<DocumentDto> AddDocumentAsync(Document document)
        {
            await _documentRepo.AddAsync(document);
            return _mapper.Map<DocumentDto>(document);
        }

        public async Task<DocumentDto> UpdateDocumentAsync(Document document)
        {
            await _documentRepo.UpdateAsync(document);
            return _mapper.Map<DocumentDto>(document);
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            Document? existing = await _documentRepo.GetByIdAsync(id);
            if (existing == null) 
                return false;

            await _documentRepo.DeleteAsync(id);
            return true;
        }

        public async Task<List<DocumentDto>> SearchDocumentsAsync(string keyword)
        {
            List<Document> documents = await _documentRepo.SearchDocumentsAsync(keyword);
            return _mapper.Map<List<DocumentDto>>(documents);
        }

        // --- Business Logic ---

        public async Task LogAccessAsync(int documentId, DateTime date)
        {
            // Use the repository directly to persist the access log
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

        public async Task AddLogToDocumentAsync(int documentId, string action, string? details = null)
        {
            DocumentLog log = new DocumentLog
            {
                Id = documentId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            await _documentLogRepo.AddAsync(log);
        }

        public async Task AddTagToDocumentAsync(int documentId, string tagName)
        {
            Document? document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null) 
                return;

            if (!document.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            {
                document.Tags.Add(new Tag { Name = tagName });
                await _documentRepo.UpdateAsync(document);
            }
        }

        public async Task RemoveTagFromDocumentAsync(int documentId, string tagName)
        {
            Document? document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null) 
                return;

            Tag? tag = document.Tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag != null)
            {
                document.Tags.Remove(tag);
                await _documentRepo.UpdateAsync(document);
            }
        }
    }
}
