using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Exceptions;
using Core.Models;
using Core.Repositories.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DAL.Repositories.Implementations
{
    public class DocumentRepository(PaperlessDBContext context, IMapper mapper, ILogger<DocumentRepository> logger) : RepositoryBase, IDocumentRepository
    {
        private readonly ILogger<DocumentRepository> _logger = logger;

        private readonly PaperlessDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public Task<List<Document>> GetAllAsync()
        {
            _logger.LogInformation("DocumentRepository.GetAllAsync called");
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentEntity> entities = await _context.Documents
                    .Include(d => d.Tags)
                    .Include(d => d.AccessLogs)
                    .Include(d => d.Logs)
                    .ToListAsync();
                return _mapper.Map<List<Document>>(entities);
            }, "Failed to retrieve all Documents.");
        }

        public Task<Document?> GetByIdAsync(int id)
        {
            _logger.LogInformation("DocumentRepository.GetByIdAsync called for ID={Id}", id);
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents
                    .Include(d => d.Tags)
                    .Include(d => d.AccessLogs)
                    .Include(d => d.Logs)
                    .FirstOrDefaultAsync(d => d.Id == id);
                return _mapper.Map<Document?>(entity);
            }, $"Failed to retrieve Document with ID {id}.");
        }

        public Task AddAsync(Document model)
        {
            _logger.LogInformation("DocumentRepository.AddAsync called for Document ID={Id}", model.Id);
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = _mapper.Map<DocumentEntity>(model);
                await _context.Documents.AddAsync(entity);
                await _context.SaveChangesAsync();
                model.Id = entity.Id;
            }, "Failed to add Document.");
        }

        public Task UpdateAsync(Document model)
        {
            _logger.LogInformation("DocumentRepository.UpdateAsync called for Document ID={Id}", model.Id);
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents
                    .Include(d => d.Tags)
                    .FirstOrDefaultAsync(d => d.Id == model.Id);

                if (entity == null)
                    throw new DataAccessException($"Document {model.Id} not found.");

                // Update basic properties (excluding Tags which we'll handle separately)
                entity.FileName = model.FileName;
                entity.FilePath = model.FilePath;
                entity.OcrText = model.OcrText;
                entity.Summary = model.Summary;
                entity.UploadedAt = model.UploadedAt;

                // Handle tags: find existing tags by name or create new ones
                if (model.Tags != null)
                {
                    List<TagEntity> tagEntities = new List<TagEntity>();

                    foreach (Tag tag in model.Tags)
                    {
                        TagEntity? tagEntity;

                        if (tag.Id > 0)
                        {
                            // Tag has an ID, try to find it
                            tagEntity = await _context.Tags.FindAsync(tag.Id);
                            if (tagEntity == null)
                            {
                                _logger.LogWarning("Tag with ID {Id} not found, creating new tag with name '{Name}'", tag.Id, tag.Name);
                                tagEntity = null; // Will create new one below
                            }
                        }
                        else
                        {
                            // Tag has no ID, try to find by name (case-insensitive)
                            tagEntity = await _context.Tags
                                .FirstOrDefaultAsync(t => t.Name.ToLower() == tag.Name.ToLower());
                        }

                        if (tagEntity == null)
                        {
                            // Create new tag
                            tagEntity = new TagEntity
                            {
                                Name = tag.Name,
                                Color = tag.Color
                            };
                            await _context.Tags.AddAsync(tagEntity);
                            _logger.LogInformation("Created new tag '{Name}' with color {Color}", tag.Name, tag.Color);
                        }
                        else
                        {
                            // Update existing tag's color if it changed
                            if (tag.Color != null && tagEntity.Color != tag.Color)
                            {
                                tagEntity.Color = tag.Color;
                                _logger.LogInformation("Updated tag '{Name}' color to {Color}", tag.Name, tag.Color);
                            }
                        }

                        tagEntities.Add(tagEntity);
                    }

                    // Replace the tags collection
                    entity.Tags = tagEntities;
                }
                else
                {
                    // If model.Tags is null, clear all tags
                    entity.Tags.Clear();
                }

                await _context.SaveChangesAsync();
            }, $"Failed to update Document with ID {model.Id}.");
        }

        public Task DeleteAsync(int id)
        {
            _logger.LogInformation("DocumentRepository.DeleteAsync called for Document ID={Id}", id);
            return ExecuteRepositoryActionAsync(async () =>
            {
                DocumentEntity? entity = await _context.Documents.FindAsync(id);
                if (entity == null)
                    throw new DataAccessException($"Document {id} not found.");

                _context.Documents.Remove(entity);
                await _context.SaveChangesAsync();
            }, $"Failed to delete Document with ID {id}.");
        }

        public Task<List<Document>> SearchDocumentsAsync(string keyword)
        {
            _logger.LogInformation("DocumentRepository.SearchDocumentsAsync called with keyword='{Keyword}'", keyword);
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentEntity> entities = await _context.Documents
                    .Include(d => d.Tags)
                    .Where(d => d.FileName.Contains(keyword) ||
                                (d.Summary != null && d.Summary.Contains(keyword)) ||
                                (d.OcrText != null && d.OcrText.Contains(keyword)))
                    .ToListAsync();
                return _mapper.Map<List<Document>>(entities);
            }, $"Failed to search Documents with keyword '{keyword}'.");
        }

        public Task<List<AccessLog>> GetAccessLogsForDocumentAsync(int documentId)
        {
            _logger.LogInformation("DocumentRepository.GetAccessLogsForDocumentAsync called for Document ID={Id}", documentId);
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<AccessLogEntity> entities = await _context.AccessLogs
                    .Where(a => a.DocumentId == documentId)
                    .ToListAsync();
                return _mapper.Map<List<AccessLog>>(entities);
            }, $"Failed to retrieve AccessLogs for Document ID {documentId}.");
        }

        public Task<List<DocumentLog>> GetLogsForDocumentAsync(int documentId)
        {
            _logger.LogInformation("DocumentRepository.GetLogsForDocumentAsync called for Document ID={Id}", documentId);
            return ExecuteRepositoryActionAsync(async () =>
            {
                List<DocumentLogEntity> entities = await _context.DocumentLogs
                    .Where(dl => dl.DocumentId == documentId)
                    .ToListAsync();
                return _mapper.Map<List<DocumentLog>>(entities);
            }, $"Failed to retrieve DocumentLogs for Document ID {documentId}.");
        }
    }
}
