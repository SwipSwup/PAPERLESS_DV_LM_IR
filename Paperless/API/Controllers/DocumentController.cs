using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using FluentValidation;
using FluentValidation.Results;
using log4net;
using System.Reflection;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(DocumentService service, IMapper mapper, IValidator<DocumentDto> validator) : ControllerBase
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DocumentDto dto)
    {
        log.Info($"DocumentController: Create called for title={dto.FileName}");

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            log.Warn($"DocumentController: Validation failed for title={dto.FileName}");
            return BadRequest(validationResult.Errors);
        }

        try
        {
            Document? model = mapper.Map<Document>(dto);
            DocumentDto created = await service.AddDocumentAsync(model);
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
}
