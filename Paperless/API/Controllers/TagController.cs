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
public class TagController(TagService service, IMapper mapper, IValidator<TagDto> validator) : ControllerBase
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        log.Info("TagController: GetAll called");

        try
        {
            List<Tag> tags = await service.GetAllTagsAsync();
            log.Info($"TagController: Returned {tags.Count} tags");
            return Ok(mapper.Map<List<TagDto>>(tags));
        }
        catch (Exception ex)
        {
            log.Error("TagController: Error in GetAll", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        log.Info($"TagController: GetById called with id={id}");

        try
        {
            Tag? tag = await service.GetTagByIdAsync(id);
            if (tag == null)
            {
                log.Warn($"TagController: No tag found with id={id}");
                return NotFound();
            }

            log.Info($"TagController: Found tag with id={id}");
            return Ok(mapper.Map<TagDto>(tag));
        }
        catch (Exception ex)
        {
            log.Error($"TagController: Error in GetById id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TagDto dto)
    {
        log.Info($"TagController: Create called for name={dto.Name}");

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            log.Warn($"TagController: Validation failed for name={dto.Name}");
            return BadRequest(validationResult.Errors);
        }

        try
        {
            Tag? tag = mapper.Map<Tag>(dto);
            Tag created = await service.AddTagAsync(tag);
            log.Info($"TagController: Tag created with id={created.Id}");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, mapper.Map<TagDto>(created));
        }
        catch (Exception ex)
        {
            log.Error("TagController: Error in Create", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TagDto dto)
    {
        log.Info($"TagController: Update called for id={id}");

        if (id != dto.Id)
        {
            log.Warn("TagController: ID mismatch in Update");
            return BadRequest("ID mismatch");
        }

        ValidationResult? validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            log.Warn($"TagController: Validation failed in Update for id={id}");
            return BadRequest(validationResult.Errors);
        }

        try
        {
            Tag? tag = mapper.Map<Tag>(dto);
            await service.UpdateTagAsync(tag);
            log.Info($"TagController: Tag updated with id={id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"TagController: Error in Update id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        log.Info($"TagController: Delete called for id={id}");

        try
        {
            await service.DeleteTagAsync(id);
            log.Info($"TagController: Tag deleted with id={id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            log.Error($"TagController: Error in Delete id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        log.Info($"TagController: Search called with keyword={keyword}");

        try
        {
            List<Tag> tags = await service.SearchTagsAsync(keyword);
            log.Info($"TagController: Search returned {tags.Count} tags");
            return Ok(mapper.Map<List<TagDto>>(tags));
        }
        catch (Exception ex)
        {
            log.Error($"TagController: Error in Search keyword={keyword}", ex);
            return StatusCode(500, ex.Message);
        }
    }
}
