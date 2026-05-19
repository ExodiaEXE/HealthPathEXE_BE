using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserRoutineController : ControllerBase
{
    private readonly IUserRoutineService _userRoutineService;

    public UserRoutineController(IUserRoutineService userRoutineService)
    {
        _userRoutineService = userRoutineService;
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> ScheduleRoutine([FromBody] CreateUserRoutineDto dto)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response = await _userRoutineService.ScheduleRoutineAsync(dto, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartRoutine(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response = await _userRoutineService.StartRoutineAsync(id, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteRoutine(Guid id, [FromBody] UserRoutineStatusUpdateDto dto)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response = await _userRoutineService.CompleteRoutineAsync(id, dto, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("{id}/fail")]
    public async Task<IActionResult> FailRoutine(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response = await _userRoutineService.FailRoutineAsync(id, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] DateTime? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response = await _userRoutineService.GetMyScheduleAsync(userId, date, page, pageSize);
        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}
