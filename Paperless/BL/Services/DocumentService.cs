using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Core.Messaging;
using Core.Models;
using Core.Repositories.Interfaces;

namespace BL.Services
{
    public class DocumentService(
        IDocumentRepository documentRepo,
        IAccessLogRepository accessLogRepo,
        IDocumentLogRepository documentLogRepo,
        IMapper mapper,
        IDocumentMessageProducer producer)
    {
        private readonly IDocumentRepository _documentRepo =
            documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));

        private readonly IAccessLogRepository _accessLogRepo =
            accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));

        private readonly IDocumentLogRepository _documentLogRepo =
            documentLogRepo ?? throw new ArgumentNullException(nameof(documentLogRepo));

        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        private readonly IDocumentMessageProducer _producer =
            producer ?? throw new ArgumentNullException(nameof(producer));

        // --- CRUD Operations ---
        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            try
            {
                List<Document> documents = await _documentRepo.GetAllAsync();
                return _mapper.Map<List<DocumentDto>>(documents);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to retrieve all documents.", ex);
            }
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(id);
                return document == null ? null : _mapper.Map<DocumentDto>(document);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to retrieve document with ID {id}.", ex);
            }
        }

        public async Task<DocumentDto> AddDocumentAsync(Document document)
        {
            try
            {
                await _documentRepo.AddAsync(document);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException("Failed to add document.", ex);
            }

            try
            {
                await _producer.PublishDocumentAsync(new DocumentMessageDto
                {
                    DocumentId = document.Id,
                    FilePath = document.FilePath,
                    FileName = document.FileName,
                    UploadedAt = document.UploadedAt
                });
            }
            catch (MessagingException ex)
            {
                // mybe add a flag to retry publishing it again later
                throw new ServiceException($"Document added but failed to publish message for Document ID {document.Id}.", ex);
            }

            return _mapper.Map<DocumentDto>(document);
        }


        public async Task<DocumentDto> UpdateDocumentAsync(Document document)
        {
            try
            {
                await _documentRepo.UpdateAsync(document);
                return _mapper.Map<DocumentDto>(document);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to update document with ID {document.Id}.", ex);
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                Document? existing = await _documentRepo.GetByIdAsync(id);
                if (existing == null)
                    return false;

                await _documentRepo.DeleteAsync(id);
                return true;
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to delete document with ID {id}.", ex);
            }
        }

        public async Task<List<DocumentDto>> SearchDocumentsAsync(string keyword)
        {
            try
            {
                List<Document> documents = await _documentRepo.SearchDocumentsAsync(keyword);
                return _mapper.Map<List<DocumentDto>>(documents);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to search documents with keyword '{keyword}'.", ex);
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

        public async Task AddLogToDocumentAsync(int documentId, string action, string? details = null)
        {
            try
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
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to add document log for Document ID {documentId}.", ex);
            }
        }

        public async Task AddTagToDocumentAsync(int documentId, string tagName)
        {
            try
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
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to add tag '{tagName}' to Document ID {documentId}.", ex);
            }
        }

        public async Task RemoveTagFromDocumentAsync(int documentId, string tagName)
        {
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(documentId);
                if (document == null)
                    return;

                Tag? tag = document.Tags.FirstOrDefault(t =>
                    t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                if (tag != null)
                {
                    document.Tags.Remove(tag);
                    await _documentRepo.UpdateAsync(document);
                }
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to remove tag '{tagName}' from Document ID {documentId}.", ex);
            }
        }
    }
}