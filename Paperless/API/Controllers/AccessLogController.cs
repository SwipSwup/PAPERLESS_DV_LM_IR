using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogController(AccessLogService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            List<AccessLogDto> logs = await service.GetAllAsync();
            return Ok(mapper.Map<List<AccessLogDto>>(logs));
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
            AccessLogDto? log = await service.GetByIdAsync(id);
            if (log == null) return NotFound();
            return Ok(mapper.Map<AccessLogDto>(log));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}