using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface INotificationService
{
    Task SendAsync(SendNotificationDto dto);
    Task SendBulkAsync(SendBulkNotificationDto dto);
    Task<ApiResponse<PageResponse<NotificationDto>>> GetMyNotificationsAsync(Guid userId, bool? unreadOnly, int page, int pageSize);
    Task<ApiResponse<object>> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId);
    Task<ApiResponse<object>> DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task<ApiResponse<UnreadCountDto>> GetUnreadCountAsync(Guid userId);
    Task<ApiResponse<NotificationSettingDto>> GetSettingsAsync(Guid userId);
    Task<ApiResponse<NotificationSettingDto>> UpdateSettingsAsync(UpdateNotificationSettingDto dto, Guid userId);
    Task<ApiResponse<object>> RegisterDeviceTokenAsync(RegisterDeviceTokenDto dto, Guid userId);
    Task<ApiResponse<object>> RemoveDeviceTokenAsync(string token, Guid userId);
}
