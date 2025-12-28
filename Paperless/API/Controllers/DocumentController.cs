using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Core.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using log4net;
using System.Reflection;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(DocumentService service, IStorageService storageService, IMapper mapper, IValidator<DocumentDto> validator) : ControllerBase
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        log.Info("DocumentController: GetAll called");

        try
        {
            List<DocumentDto> documents = await service.GetAllDocumentsAsync();
            log.Info($"DocumentController: Returned {documents.Count} documents");
            return Ok(mapper.Map<List<DocumentDto>>(documents));
        }
        catch (Exception ex)
        {
            log.Error("DocumentController: Error in GetAll", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        log.Info($"DocumentController: GetById called with id={id}");

        try
        {
            DocumentDto? document = await service.GetDocumentByIdAsync(id);
            if (document == null)
            {
                log.Warn($"DocumentController: No document found with id={id}");
                return NotFound();
            }

            log.Info($"DocumentController: Found document with id={id}");
            return Ok(mapper.Map<DocumentDto>(document));
        }
        catch (Exception ex)
        {
            log.Error($"DocumentController: Error in GetById id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        log.Info($"DocumentController: Download called for id={id}");
        try
        {
            DocumentDto? document = await service.GetDocumentByIdAsync(id);
            if (document == null)
            {
                 log.Warn($"DocumentController: Document {id} not found for download.");
                 return NotFound();
            }

            if (string.IsNullOrEmpty(document.FilePath))
            {
                 log.Warn($"DocumentController: Document {id} has no file path.");
                 return NotFound("File path is missing.");
            }

            Stream stream = await storageService.GetFileAsync(document.FilePath);
            // Determine content type based on extension or default to octet-stream/pdf
            string contentType = "application/octet-stream";
            string ext = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
            else if (ext == ".png") contentType = "image/png";
            else if (ext == ".txt") contentType = "text/plain";

            // Set Content-Disposition to inline to allow browser preview
            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = document.FileName,
                Inline = true  // This forces the browser to try and open it
            };
            Response.Headers.Append("Content-Disposition", cd.ToString());

            return File(stream, contentType);
        }
        catch (Exception ex)
        {
            log.Error($"DocumentController: Error in Download id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] DocumentUploadDto uploadDto)
    {
        log.Info($"DocumentController: Create called for file={uploadDto.File.FileName}");

        if (uploadDto.File.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            // 1. Upload file to MinIO
            string fileName = $"{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Day:D2}/{Guid.NewGuid()}_{uploadDto.File.FileName}";
            using (Stream stream = uploadDto.File.OpenReadStream())
            {
                await storageService.UploadFileAsync(stream, fileName, uploadDto.File.ContentType);
            }

            // 2. Create Document Entity
            Document document = new Document
            {
                FileName = uploadDto.Title ?? uploadDto.File.FileName,
                FilePath = fileName, // Store the object name/path in MinIO
                UploadedAt = DateTime.UtcNow,
                Tags = new List<Tag>() // Handle tags if provided
            };
            
            if (uploadDto.Tags != null && uploadDto.Tags.Any())
            {
                 // In a real app, you might want to fetch existing tags or create new ones here.
                 // For now, we'll map string tags to Tag entities if possible or let Service handle it.
                 // Since DocumentService.AddDocumentAsync takes a Document, we'll let it be.
                 // Simplified: Just add them as new Tag objects for now.
                 foreach(string tag in uploadDto.Tags)
                 {
                     document.Tags.Add(new Tag { Name = tag });
                 }
            }

            // 3. Save to DB (and publish RabbitMQ message)
            DocumentDto created = await service.AddDocumentAsync(document);
            
            log.Info($"DocumentController: Document created with id={created.Id}");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<DocumentDto>(created));
        }
        catch (Exception ex)
        {
            log.Error("DocumentController: Error in Create", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dto)
    {
        log.Info($"DocumentController: Update called for id={id}");

        if (id != dto.Id)
        {
            log.Warn("DocumentController: ID mismatch in Update");
            return BadRequest("ID mismatch");
        }

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            log.Warn($"DocumentController: Validation failed in Update for id={id}");
            return BadRequest(validationResult.Errors);
        }

        try
        {
            Document? model = mapper.Map<Document>(dto);
            await service.UpdateDocumentAsync(model);
            log.Info($"DocumentController: Document updated with id={id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"DocumentController: Error in Update id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        log.Info($"DocumentController: Delete called for id={id}");

        try
        {
            // Note: Should also delete from Storage, but keeping it simple for now
            await service.DeleteDocumentAsync(id);
            log.Info($"DocumentController: Document deleted with id={id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"DocumentController: Error in Delete id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        log.Info($"DocumentController: Search called with keyword={keyword}");

        try
        {
            List<DocumentDto> documents = await service.SearchDocumentsAsync(keyword);
            log.Info($"DocumentController: Search returned {documents.Count} documents");
            return Ok(mapper.Map<List<DocumentDto>>(documents));
        }
        catch (Exception ex)
        {
            log.Error($"DocumentController: Error in Search keyword={keyword}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("{id}/tags")]
    public async Task<IActionResult> AddTag(int id, [FromBody] TagDto tag)
    {
        log.Info($"DocumentController: AddTag called for id={id} tag={tag.Name}");
        try
        {
            await service.AddTagToDocumentAsync(id, tag);
            return Ok();
        }
        catch (Exception ex)
        {
             log.Error($"DocumentController: Error adding tag to id={id}", ex);
             return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(int id, string tag)
    {
        log.Info($"DocumentController: RemoveTag called for id={id} tag={tag}");
        try
        {
            await service.RemoveTagFromDocumentAsync(id, tag);
            return Ok();
        }
        catch (Exception ex)
        {
             log.Error($"DocumentController: Error removing tag from id={id}", ex);
             return StatusCode(500, ex.Message);
        }
    }
}
