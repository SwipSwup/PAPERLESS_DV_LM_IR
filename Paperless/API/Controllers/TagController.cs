using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using FluentValidation;
using FluentValidation.Results;
using Serilog;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController(TagService service, IMapper mapper, IValidator<TagDto> validator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        Log.Information("TagController: GetAll called");
        List<Tag> tags = await service.GetAllTagsAsync();
        Log.Information("TagController: Returned {Count} tags", tags.Count);
        return Ok(mapper.Map<List<TagDto>>(tags));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Log.Information("TagController: GetById called with id={Id}", id);
        Tag? tag = await service.GetTagByIdAsync(id);
        if (tag == null)
        {
            Log.Warning("TagController: No tag found with id={Id}", id);
            return NotFound();
        }

        Log.Information("TagController: Found tag with id={Id}", id);
        return Ok(mapper.Map<TagDto>(tag));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TagDto dto)
    {
        Log.Information("TagController: Create called for name={Name}", dto.Name);

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            Log.Warning("TagController: Validation failed for name={Name}", dto.Name);
            return BadRequest(validationResult.Errors);
        }

        Tag? tag = mapper.Map<Tag>(dto);
        Tag created = await service.AddTagAsync(tag);
        Log.Information("TagController: Tag created with id={Id}", created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<TagDto>(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TagDto dto)
    {
        Log.Information("TagController: Update called for id={Id}", id);

        if (id != dto.Id)
        {
            Log.Warning("TagController: ID mismatch in Update");
            return BadRequest("ID mismatch");
        }

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            Log.Warning("TagController: Validation failed in Update for id={Id}", id);
            return BadRequest(validationResult.Errors);
        }

        Tag? tag = mapper.Map<Tag>(dto);
        await service.UpdateTagAsync(tag);
        Log.Information("TagController: Tag updated with id={Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        Log.Information("TagController: Delete called for id={Id}", id);
        await service.DeleteTagAsync(id);
        Log.Information("TagController: Tag deleted with id={Id}", id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        Log.Information("TagController: Search called with keyword={Keyword}", keyword);
        List<Tag> tags = await service.SearchTagsAsync(keyword);
        Log.Information("TagController: Search returned {Count} tags", tags.Count);
        return Ok(mapper.Map<List<TagDto>>(tags));
    }
}
