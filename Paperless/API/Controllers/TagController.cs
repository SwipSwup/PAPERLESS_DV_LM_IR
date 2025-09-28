using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController(TagService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await service.GetAllTagsAsync();
        return Ok(mapper.Map<List<TagDto>>(tags));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tag = await service.GetTagByIdAsync(id);
        if (tag == null) return NotFound();
        return Ok(mapper.Map<TagDto>(tag));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TagDto dto)
    {
        var tag = mapper.Map<Tag>(dto);
        var created = await service.AddTagAsync(tag);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<TagDto>(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TagDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        var tag = mapper.Map<Tag>(dto);
        await service.UpdateTagAsync(tag);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteTagAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var tags = await service.SearchTagsAsync(keyword);
        return Ok(mapper.Map<List<TagDto>>(tags));
    }
}