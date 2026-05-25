using System.Threading.Tasks;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var response = await _dashboardService.GetDashboardStatsAsync();
        return Ok(response);
    }
}
