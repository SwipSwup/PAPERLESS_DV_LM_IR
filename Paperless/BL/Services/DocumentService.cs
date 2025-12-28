using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Core.Messaging;
using Core.Models;
using Core.Repositories.Interfaces;
using log4net;
using System.Reflection;

namespace BL.Services
{
    public class DocumentService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IDocumentRepository _documentRepo;
        private readonly IAccessLogRepository _accessLogRepo;
        private readonly IDocumentLogRepository _documentLogRepo;
        private readonly IMapper _mapper;
        private readonly IDocumentMessageProducer _producer;

        public DocumentService(
            IDocumentRepository documentRepo,
            IAccessLogRepository accessLogRepo,
            IDocumentLogRepository documentLogRepo,
            IMapper mapper,
            IDocumentMessageProducer producer)
        {
            _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
            _accessLogRepo = accessLogRepo ?? throw new ArgumentNullException(nameof(accessLogRepo));
            _documentLogRepo = documentLogRepo ?? throw new ArgumentNullException(nameof(documentLogRepo));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));

            log.Info("DocumentService initialized");
        }

        // --- CRUD Operations ---
        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            log.Info("DocumentService.GetAllDocumentsAsync called");
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
            log.Info($"DocumentService.GetDocumentByIdAsync called with ID={id}");
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
            log.Info($"DocumentService.AddDocumentAsync called for Document ID={document.Id}");
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
                log.Info($"DocumentService: Publishing message for Document ID={document.Id}");
                await _producer.PublishDocumentAsync(new DocumentMessageDto
                {
                    DocumentId = document.Id,
                    FilePath = document.FilePath,
                    FileName = document.FileName,
                    UploadedAt = document.UploadedAt
                });
                log.Info($"DocumentService: Published message for Document ID={document.Id}");
            }
            catch (MessagingException ex)
            {
                throw new ServiceException($"Document added but failed to publish message for Document ID {document.Id}.", ex);
            }

            return _mapper.Map<DocumentDto>(document);
        }

        public async Task<DocumentDto> UpdateDocumentAsync(Document document)
        {
            log.Info($"DocumentService.UpdateDocumentAsync called for Document ID={document.Id}");
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
            log.Info($"DocumentService.DeleteDocumentAsync called for ID={id}");
            try
            {
                Document? existing = await _documentRepo.GetByIdAsync(id);
                if (existing == null)
                {
                    log.Warn($"DocumentService.DeleteDocumentAsync: Document ID={id} not found");
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
            log.Info($"DocumentService.SearchDocumentsAsync called with keyword='{keyword}'");
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
            log.Info($"DocumentService.LogAccessAsync called for Document ID={documentId} Date={date:yyyy-MM-dd}");
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
            log.Info($"DocumentService.AddLogToDocumentAsync called for Document ID={documentId} Action='{action}'");
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
            log.Info($"DocumentService.AddTagToDocumentAsync called for Document ID={documentId} Tag='{tagDto.Name}'");
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(documentId);
                if (document == null)
                {
                    log.Warn($"DocumentService.AddTagToDocumentAsync: Document ID={documentId} not found");
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
            log.Info($"DocumentService.RemoveTagFromDocumentAsync called for Document ID={documentId} Tag='{tagName}'");
            try
            {
                Document? document = await _documentRepo.GetByIdAsync(documentId);
                if (document == null)
                {
                    log.Warn($"DocumentService.RemoveTagFromDocumentAsync: Document ID={documentId} not found");
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
