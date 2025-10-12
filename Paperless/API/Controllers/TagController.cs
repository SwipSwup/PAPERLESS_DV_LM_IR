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
public class TagController(TagService service, IMapper mapper, IValidator<TagDto> validator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            List<Tag> tags = await service.GetAllTagsAsync();
            
            return Ok(mapper.Map<List<TagDto>>(tags));
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
            Tag? tag = await service.GetTagByIdAsync(id);
            if (tag == null) 
                return NotFound();
            
            return Ok(mapper.Map<TagDto>(tag));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TagDto dto)
    {
        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            Tag? tag = mapper.Map<Tag>(dto);
            Tag created = await service.AddTagAsync(tag);
            
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<TagDto>(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TagDto dto)
    {
        if (id != dto.Id) 
            return BadRequest("ID mismatch");

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            Tag? tag = mapper.Map<Tag>(dto);
            await service.UpdateTagAsync(tag);
            
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
            await service.DeleteTagAsync(id);
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
            List<Tag> tags = await service.SearchTagsAsync(keyword);
            return Ok(mapper.Map<List<TagDto>>(tags));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}