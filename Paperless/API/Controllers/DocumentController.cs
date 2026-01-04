using BL.Services;
using AutoMapper;
using Core.DTOs;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Core.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;


namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(
    DocumentService service,
    IStorageService storageService,
    IMapper mapper,
    IValidator<DocumentDto> validator) : ControllerBase
{
    /// <summary>
    /// Retrieves all documents.
    /// </summary>
    /// <returns>A list of all documents.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        Log.Information("DocumentController: GetAll called");
        List<DocumentDto> documents = await service.GetAllDocumentsAsync();
        return Ok(mapper.Map<List<DocumentDto>>(documents));
    }

    /// <summary>
    /// Retrieves a specific document by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document.</param>
    /// <returns>The requested document if found; otherwise, NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Log.Information("DocumentController: GetById called with id={Id}", id);
        DocumentDto? document = await service.GetDocumentByIdAsync(id);
        if (document == null)
            throw new EntityNotFoundException(nameof(Document), id);

        return Ok(mapper.Map<DocumentDto>(document));
    }

    /// <summary>
    /// Downloads the file associated with a document.
    /// </summary>
    /// <param name="id">The unique identifier of the document.</param>
    /// <returns>The file stream of the document.</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        Log.Information("DocumentController: Download called for id={Id}", id);

        DocumentDto? document = await service.GetDocumentByIdAsync(id);
        if (document == null)
            throw new EntityNotFoundException(nameof(Document), id);

        if (string.IsNullOrEmpty(document.FilePath))
        {
            Log.Warning("DocumentController: Document {Id} has no file path.", id);
            return NotFound("File path is missing.");
        }

        Stream stream = await storageService.GetFileAsync(document.FilePath);
        // Determine content type based on extension or default to octet-stream/pdf
        string contentType = "application/octet-stream";
        string ext = Path.GetExtension(document.FileName).ToLowerInvariant();

        contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        // Set Content-Disposition to inline to allow browser preview
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = document.FileName,
            Inline = true // This forces the browser to try and open it
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());

        return File(stream, contentType);
    }

    /// <summary>
    /// Uploads a new document.
    /// </summary>
    /// <param name="uploadDto">The document upload data transfer object containing the file and metadata.</param>
    /// <returns>The created document.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] DocumentUploadDto uploadDto)
    {
        // 1. Validation (Should technically be in FluentValidation, but keeping rudimentary check)
        if (uploadDto.File == null || uploadDto.File.Length == 0)
            throw new DmsValidationException("No file uploaded.");

        Log.Information("DocumentController: Create called for file={FileName}", uploadDto.File.FileName);

        // 2. Upload file to MinIO
        string fileName =
            $"{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Day:D2}/{Guid.NewGuid()}_{uploadDto.File.FileName}";
        using (Stream stream = uploadDto.File.OpenReadStream())
        {
            await storageService.UploadFileAsync(stream, fileName, uploadDto.File.ContentType);
        }

        // 3. Create Document Entity
        Document document = new Document
        {
            FileName = uploadDto.Title ?? uploadDto.File.FileName,
            FilePath = fileName,
            UploadedAt = DateTime.UtcNow,
            Tags = new List<Tag>()
        };

        if (uploadDto.Tags != null && uploadDto.Tags.Any())
        {
            foreach (string tag in uploadDto.Tags)
            {
                document.Tags.Add(new Tag { Name = tag });
            }
        }

        // 4. Save to DB (and publish RabbitMQ message) with Compensating Transaction
        try
        {
            DocumentDto created = await service.AddDocumentAsync(document);
            Log.Information("DocumentController: Document created with id={Id}", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<DocumentDto>(created));
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "DocumentController: Failed to save document metadata. Executing compensating transaction (deleting file).");
            // COMPENSATING ACTION: Delete the file from Storage to prevent orphans
            try
            {
                await storageService.DeleteFileAsync(fileName);
                Log.Information("DocumentController: Compensating transaction successful. File {FileName} deleted.",
                    fileName);
            }
            catch (Exception deleteEx)
            {
                // Critical failure: Both DB save failed AND cleanup failed. 
                // In a real system, we'd log this to a special "Orphans" table or alert.
                Log.Fatal(deleteEx,
                    "DocumentController: Compensating transaction FAILED. File {FileName} is orphaned in MinIO.",
                    fileName);
            }

            throw; // Re-throw original exception to return 500
        }
    }

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The unique identifier of the document to update.</param>
    /// <param name="dto">The updated document data.</param>
    /// <returns>NoContent if successful.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dto)
    {
        Log.Information("DocumentController: Update called for id={Id}", id);

        if (id != dto.Id)
        {
            Log.Warning("DocumentController: ID mismatch in Update");
            return BadRequest("ID mismatch");
        }

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            Log.Warning("DocumentController: Validation failed in Update for id={Id}", id);
            return BadRequest(validationResult.Errors);
        }

        Document? model = mapper.Map<Document>(dto);
        await service.UpdateDocumentAsync(model);
        Log.Information("DocumentController: Document updated with id={Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Deletes a document by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the document to delete.</param>
    /// <returns>NoContent if successful.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        Log.Information("DocumentController: Delete called for id={Id}", id);

        // Note: Should also delete from Storage, but keeping it simple for now
        await service.DeleteDocumentAsync(id);
        Log.Information("DocumentController: Document deleted with id={Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Searches for documents based on a keyword.
    /// </summary>
    /// <param name="keyword">The keyword to search for.</param>
    /// <returns>A list of documents matching the search criteria.</returns>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        Log.Information("DocumentController: Search called with keyword={Keyword}", keyword);
        List<DocumentDto> documents = await service.SearchDocumentsAsync(keyword);
        Log.Information("DocumentController: Search returned {Count} documents", documents.Count);
        return Ok(mapper.Map<List<DocumentDto>>(documents));
    }

    [HttpPost("{id}/tags")]
    public async Task<IActionResult> AddTag(int id, [FromBody] TagDto tag)
    {
        Log.Information("DocumentController: AddTag called for id={Id} tag={TagName}", id, tag.Name);
        await service.AddTagToDocumentAsync(id, tag);
        return Ok();
    }

    [HttpDelete("{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(int id, string tag)
    {
        Log.Information("DocumentController: RemoveTag called for id={Id} tag={TagName}", id, tag);
        await service.RemoveTagFromDocumentAsync(id, tag);
        return Ok();
    }
}