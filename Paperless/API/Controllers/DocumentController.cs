using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using FluentValidation;
using FluentValidation.Results;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(DocumentService service, IMapper mapper, IValidator<DocumentDto> validator) : ControllerBase
{
    [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                List<DocumentDto> documents = await service.GetAllDocumentsAsync();
                return Ok(mapper.Map<List<DocumentDto>>(documents));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                DocumentDto? document = await service.GetDocumentByIdAsync(id);
                if (document == null) 
                    return NotFound();
                
                return Ok(mapper.Map<DocumentDto>(document));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDto dto)
        {
            ValidationResult? validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            try
            {
                Document? model = mapper.Map<Document>(dto);
                DocumentDto created = await service.AddDocumentAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<DocumentDto>(created));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dto)
        {
            if (id != dto.Id) 
                return BadRequest("ID mismatch");

            ValidationResult? validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            try
            {
                Document? model = mapper.Map<Document>(dto);
                await service.UpdateDocumentAsync(model);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await service.DeleteDocumentAsync(id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            try
            {
                List<DocumentDto> documents = await service.SearchDocumentsAsync(keyword);
                
                return Ok(mapper.Map<List<DocumentDto>>(documents));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
}
