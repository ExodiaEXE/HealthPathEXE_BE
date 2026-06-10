using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutineController : ControllerBase
{
    private readonly IRoutineService _routineService;

    public RoutineController(IRoutineService routineService)
    {
        _routineService = routineService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutines(
        [FromQuery] string? category,
        [FromQuery] string? difficulty,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await _routineService.GetRoutinesAsync(category, difficulty, page, pageSize);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoutine(Guid id)
    {
        var response = await _routineService.GetRoutineByIdAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineDto dto)
    {
        // Admin token → routine hệ thống (CreatedBy null). User token → routine cá nhân.
        if (User.IsAdminToken())
        {
            var response = await _routineService.CreateRoutineAsync(dto, createdBy: null, isSystem: true);
            return Ok(response);
        }

        var userId = User.GetUserId();
        var userResponse = await _routineService.CreateRoutineAsync(dto, createdBy: userId, isSystem: false);
        return Ok(userResponse);
    }
}
