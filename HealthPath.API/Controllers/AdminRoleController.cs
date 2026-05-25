using System;
using System.Threading.Tasks;
using HealthPath.API.Filters;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "AdminOnly")]
public class AdminRoleController : ControllerBase
{
    private readonly IAdminRoleService _roleService;

    public AdminRoleController(IAdminRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequirePermission("view_roles")]
    public async Task<IActionResult> GetRoles()
    {
        var response = await _roleService.GetRolesAsync();
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("permissions")]
    [RequirePermission("view_permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var response = await _roleService.GetPermissionsAsync();
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}/permissions")]
    [RequirePermission("view_roles")]
    public async Task<IActionResult> GetRoleWithPermissions(Guid id)
    {
        var response = await _roleService.GetRoleWithPermissionsAsync(id);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("assign-permissions")]
    [RequirePermission("manage_roles")]
    public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionDto request)
    {
        var response = await _roleService.AssignPermissionsToRoleAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
