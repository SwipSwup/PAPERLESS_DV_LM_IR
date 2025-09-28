using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Core.Models;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogController(AccessLogService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await service.GetAllAsync();
        return Ok(mapper.Map<List<AccessLogDto>>(logs));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await service.GetByIdAsync(id);
        if (log == null) return NotFound();
        return Ok(mapper.Map<AccessLogDto>(log));
    }
}