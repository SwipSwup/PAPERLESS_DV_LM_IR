using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogController(AccessLogService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        Log.Information("AccessLogController: GetAll called");
        List<AccessLogDto> logs = await service.GetAllAsync();
        Log.Information("AccessLogController: Returned {Count} logs", logs.Count);
        return Ok(mapper.Map<List<AccessLogDto>>(logs));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Log.Information("AccessLogController: GetById called with id={Id}", id);

        AccessLogDto? logDto = await service.GetByIdAsync(id);
        if (logDto == null)
        {
            Log.Warning("AccessLogController: No log found with id={Id}", id);
            return NotFound();
        }

        Log.Information("AccessLogController: Found log with id={Id}", id);
        return Ok(mapper.Map<AccessLogDto>(logDto));
    }
}