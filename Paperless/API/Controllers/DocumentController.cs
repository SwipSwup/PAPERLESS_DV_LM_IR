using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(DocumentService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = await service.GetAllDocumentsAsync();
        var dtos = mapper.Map<List<DocumentDto>>(documents);
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var document = await service.GetDocumentByIdAsync(id);
        if (document == null) return NotFound();
        var dto = mapper.Map<DocumentDto>(document);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DocumentDto dto)
    {
        var model = mapper.Map<Document>(dto);
        var created = await service.AddDocumentAsync(model);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<DocumentDto>(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        var model = mapper.Map<Document>(dto);
        await service.UpdateDocumentAsync(model);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteDocumentAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var documents = await service.SearchDocumentsAsync(keyword);
        var dtos = mapper.Map<List<DocumentDto>>(documents);
        return Ok(dtos);
    }
}
