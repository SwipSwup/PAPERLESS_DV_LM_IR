using BL.Services;
using AutoMapper;
using Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using log4net;
using System.Reflection;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogController(AccessLogService service, IMapper mapper) : ControllerBase
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        log.Info("AccessLogController: GetAll called");

        try
        {
            List<AccessLogDto> logs = await service.GetAllAsync();
            log.Info($"AccessLogController: Returned {logs.Count} logs");
            return Ok(mapper.Map<List<AccessLogDto>>(logs));
        }
        catch (Exception ex)
        {
            log.Error("AccessLogController: Error in GetAll", ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        log.Info($"AccessLogController: GetById called with id={id}");

        try
        {
            AccessLogDto? logDto = await service.GetByIdAsync(id);
            if (logDto == null)
            {
                log.Warn($"AccessLogController: No log found with id={id}");
                return NotFound();
            }

            log.Info($"AccessLogController: Found log with id={id}");
            return Ok(mapper.Map<AccessLogDto>(logDto));
        }
        catch (Exception ex)
        {
            log.Error($"AccessLogController: Error in GetById id={id}", ex);
            return StatusCode(500, ex.Message);
        }
    }
}