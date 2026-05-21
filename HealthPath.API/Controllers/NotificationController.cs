using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool? unreadOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.GetMyNotificationsAsync(userId, unreadOnly, page, pageSize);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        var response = await _notificationService.GetUnreadCountAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.MarkAsReadAsync(id, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        var response = await _notificationService.MarkAllAsReadAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.DeleteNotificationAsync(id, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = User.GetUserId();
        var response = await _notificationService.GetSettingsAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNotificationSettingDto dto)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.UpdateSettingsAsync(dto, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.RegisterDeviceTokenAsync(dto, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("device-token")]
    public async Task<IActionResult> RemoveDeviceToken([FromQuery] string token)
    {
        var userId = User.GetUserId();
        var response = await _notificationService.RemoveDeviceTokenAsync(token, userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }
}
