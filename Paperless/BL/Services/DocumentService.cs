using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Core.Messaging;
using Core.Models;
using Core.Repositories.Interfaces;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BL.Services
{
    public class DocumentService
    {
        private readonly ILogger<DocumentService> _logger;

        private readonly IDocumentRepository _documentRepo;
        private readonly IAccessLogRepository _accessLogRepo;
        private readonly IDocumentLogRepository _documentLogRepo;
        private readonly IMapper _mapper;
        private readonly IDocumentMessageProducer _producer;
        private readonly ISearchService _searchService;

        public DocumentService(
            IDocumentRepository documentRepo,
            IAccessLogRepository accessLogRepo,
            IDocumentLogRepository documentLogRepo,
            IMapper mapper,
            IDocumentMessageProducer producer,
            ISearchService searchService,
            ILogger<DocumentService> logger)
        {
            _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
            _accessLogRepo = accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));
            _documentLogRepo = documentLogRepo ?? throw new ArgumentNullException(nameof(documentLogRepo));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _logger = logger;

            _logger.LogInformation("DocumentService initialized");
        }

        // --- CRUD Operations ---
        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            _logger.LogInformation("DocumentService.GetAllDocumentsAsync called");
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
            _logger.LogInformation("DocumentService.GetDocumentByIdAsync called with ID={Id}", id);
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
            _logger.LogInformation("DocumentService.AddDocumentAsync called for Document ID={Id}", document.Id);
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
                _logger.LogInformation("DocumentService: Publishing message for Document ID={Id}", document.Id);
                await _producer.PublishDocumentAsync(new DocumentMessageDto
                {
                    DocumentId = document.Id,
                    FilePath = document.FilePath,
                    FileName = document.FileName,
                    UploadedAt = document.UploadedAt
                });
                _logger.LogInformation("DocumentService: Published message for Document ID={Id}", document.Id);
            }
            catch (MessagingException ex)
            {
                throw new ServiceException($"Document added but failed to publish message for Document ID {document.Id}.", ex);
            }

            return _mapper.Map<DocumentDto>(document);
        }

        public async Task<DocumentDto> UpdateDocumentAsync(Document document)
        {
            _logger.LogInformation("DocumentService.UpdateDocumentAsync called for Document ID={Id}", document.Id);
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
            _logger.LogInformation("DocumentService.DeleteDocumentAsync called for ID={Id}", id);
            try
            {
                Document? existing = await _documentRepo.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("DocumentService.DeleteDocumentAsync: Document ID={Id} not found", id);
                    return false;
                }

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
            _logger.LogInformation("DocumentService.SearchDocumentsAsync called with keyword='{Keyword}'", keyword);
            try
            {
                IEnumerable<DocumentDto> dtos = await _searchService.SearchDocumentsAsync(keyword);
                return dtos.ToList();
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to search documents with keyword '{keyword}'.", ex);
            }
        }

        // --- Business Logic ---
        public async Task LogAccessAsync(int documentId, DateTime date)
        {
            _logger.LogInformation("DocumentService.LogAccessAsync called for Document ID={Id} Date={Date}", documentId, date.ToString("yyyy-MM-dd"));
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

        public async Task AddLogToDocumentAsync(int documentId, string action, string? details = null)
        {
            _logger.LogInformation("DocumentService.AddLogToDocumentAsync called for Document ID={Id} Action='{Action}'", documentId, action);
            try
            {
                DocumentLog logEntry = new DocumentLog
                {
                    Id = documentId,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };
                await _documentLogRepo.AddAsync(logEntry);
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to add document log for Document ID {documentId}.", ex);
            }
        }

        public async Task AddTagToDocumentAsync(int documentId, TagDto tagDto)
        {
            _logger.LogInformation("DocumentService.AddTagToDocumentAsync called for Document ID={Id} Tag='{TagName}'", documentId, tagDto.Name);
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("DocumentService.AddTagToDocumentAsync: Document ID={Id} not found", documentId);
                    return;
                }

                if (!document.Tags.Any(t => t.Name.Equals(tagDto.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    document.Tags.Add(new Tag { Name = tagDto.Name, Color = tagDto.Color });
                    await _documentRepo.UpdateAsync(document);
                }
            }
            catch (DataAccessException ex)
            {
                throw new ServiceException($"Failed to add tag '{tagDto.Name}' to Document ID {documentId}.", ex);
            }
        }

        public async Task RemoveTagFromDocumentAsync(int documentId, string tagName)
        {
            _logger.LogInformation("DocumentService.RemoveTagFromDocumentAsync called for Document ID={Id} Tag='{TagName}'", documentId, tagName);
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("DocumentService.RemoveTagFromDocumentAsync: Document ID={Id} not found", documentId);
                    return;
                }

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
