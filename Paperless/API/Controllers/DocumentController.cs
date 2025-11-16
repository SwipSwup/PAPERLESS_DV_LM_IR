using BL.Services;
using Core.DTOs;
using Core.Models;
using DAL.Repositories.Implementations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using log4net;
using System.Reflection;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly DocumentService service;
    private readonly IMapper mapper;
    private readonly IValidator<DocumentDto> validator;
    private readonly MinioDocumentRepository minioRepo;
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    public DocumentController(
        DocumentService service,
        IMapper mapper,
        IValidator<DocumentDto> validator,
        MinioDocumentRepository minioRepo)
    {
        this.service = service;
        this.mapper = mapper;
        this.validator = validator;
        this.minioRepo = minioRepo;
    }

    // -------------------- CRUD --------------------
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var documents = await service.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            log.Error("GetAll error", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var document = await service.GetDocumentByIdAsync(id);
            if (document == null) return NotFound();
            return Ok(document);
        }
        catch (Exception ex)
        {
            log.Error($"GetById error id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DocumentDto dto)
    {
        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            var model = mapper.Map<Document>(dto);
            var created = await service.AddDocumentAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            log.Error("Create error", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            var model = mapper.Map<Document>(dto);
            await service.UpdateDocumentAsync(model);
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"Update error id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var documentDto = await service.GetDocumentByIdAsync(id);
            if (documentDto == null) return NotFound();

            var documentModel = mapper.Map<Document>(documentDto);
            await minioRepo.DeleteAsync(documentModel);
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"DeleteDocument error id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        try
        {
            var documents = await service.SearchDocumentsAsync(keyword);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            log.Error($"Search error keyword={keyword}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    // -------------------- MinIO Upload --------------------
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromQuery] int userId)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        var tempPath = Path.GetTempFileName();
        await using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream);
        }

        var uploadedDocument = await minioRepo.UploadAsync(tempPath, userId);
        var dto = mapper.Map<DocumentDto>(uploadedDocument);
        return Ok(dto);
    }

    // -------------------- MinIO Download --------------------
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var documentDto = await service.GetDocumentByIdAsync(id);
        if (documentDto == null) return NotFound();

        var documentModel = mapper.Map<Document>(documentDto);
        var tempPath = Path.Combine(Path.GetTempPath(), documentDto.FileName);

        await minioRepo.DownloadAsync(documentModel, tempPath);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(tempPath);
        System.IO.File.Delete(tempPath);

        return File(fileBytes, "application/octet-stream", documentDto.FileName);
    }
}
