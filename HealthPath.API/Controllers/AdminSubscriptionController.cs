using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Route("api/admin/subscriptions")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminSubscriptionController : ControllerBase
{
    private readonly IAdminSubscriptionService _adminSubscriptionService;

    public AdminSubscriptionController(IAdminSubscriptionService adminSubscriptionService)
    {
        _adminSubscriptionService = adminSubscriptionService;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetAllPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _adminSubscriptionService.GetAllPlansAsync(page, pageSize);
        return Ok(response);
    }

    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var response = await _adminSubscriptionService.GetPlanByIdAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanDto planDto)
    {

        var response = await _adminSubscriptionService.CreatePlanAsync(planDto);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateSubscriptionPlanDto planDto)
    {

        var response = await _adminSubscriptionService.UpdatePlanAsync(id, planDto);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var response = await _adminSubscriptionService.DeletePlanAsync(id);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? search,
        [FromQuery] string? platform,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _adminSubscriptionService.GetTransactionsPagedAsync(search, platform, status, page, pageSize);
        return Ok(response);
    }
}
