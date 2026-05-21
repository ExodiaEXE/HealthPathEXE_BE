using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services.Channels;
using Hangfire;

namespace HealthPath.API.Services;

public class NotificationService : INotificationService
{
    private readonly HealthpathDbContext _dbContext;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        HealthpathDbContext dbContext,
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _channels = channels;
        _logger = logger;
    }

    public async Task SendAsync(SendNotificationDto dto)
    {
        var user = await _dbContext.Users
            .Include(u => u.NotificationSetting)
            .Include(u => u.DeviceTokens)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found. Skipping notification.", dto.UserId);
            return;
        }

        // Get or initialize user settings
        var settings = user.NotificationSetting;
        if (settings == null)
        {
            settings = new NotificationSetting
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DailyCheckin = true,
                StreakReminder = true,
                GroupActivity = true,
                ChallengeUpdates = true,
                Promotions = true,
                PushEnabled = true,
                EmailEnabled = true,
                InAppEnabled = true,
                QuietFrom = new TimeOnly(22, 0),
                QuietUntil = new TimeOnly(7, 0),
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.NotificationSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
            user.NotificationSetting = settings;
        }

        // 1. Check if user enabled this notification type
        bool isTypeEnabled = dto.Type.ToLower() switch
        {
            "daily_checkin" => settings.DailyCheckin,
            "streak_alert" => settings.StreakReminder,
            "group_activity" => settings.GroupActivity,
            "challenge_update" => settings.ChallengeUpdates,
            "promotion" => settings.Promotions,
            _ => true
        };

        if (!isTypeEnabled)
        {
            _logger.LogInformation("Notification type {Type} is disabled for User {UserId}.", dto.Type, user.Id);
            return;
        }

        // 2. Prepare notifications to save
        var enabledChannels = new List<string>();
        if (settings.InAppEnabled) enabledChannels.Add("in_app");
        if (settings.PushEnabled && user.DeviceTokens != null && user.DeviceTokens.Any(t => t.IsActive)) enabledChannels.Add("push");
        if (settings.EmailEnabled) enabledChannels.Add("email");

        if (!enabledChannels.Any())
        {
            _logger.LogInformation("No channels enabled for User {UserId}.", user.Id);
            return;
        }

        // Save notification record to DB for each enabled channel
        var savedNotifications = new List<Notification>();
        foreach (var chan in enabledChannels)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = dto.Type,
                Title = dto.Title,
                Body = dto.Body,
                Data = dto.Data ?? "{}",
                Channel = chan,
                IsRead = false,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Notifications.Add(notification);
            savedNotifications.Add(notification);
        }
        await _dbContext.SaveChangesAsync();

        // 3. Check Quiet Hours
        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var currentTime = TimeOnly.FromDateTime(localTime);

        bool inQuietHours = false;
        if (settings.QuietFrom.HasValue && settings.QuietUntil.HasValue)
        {
            var from = settings.QuietFrom.Value;
            var until = settings.QuietUntil.Value;
            if (from < until)
            {
                inQuietHours = currentTime >= from && currentTime <= until;
            }
            else
            {
                inQuietHours = currentTime >= from || currentTime <= until;
            }
        }

        if (inQuietHours)
        {
            _logger.LogInformation("Quiet hours active for User {UserId}. Deferring notifications.", user.Id);
            // Calculate delay until quiet hours end
            var untilTime = settings.QuietUntil!.Value;
            var targetDateTime = localTime.Date.Add(untilTime.ToTimeSpan());
            if (currentTime >= untilTime)
            {
                // If it is past quiet until today, target is tomorrow
                targetDateTime = targetDateTime.AddDays(1);
            }
            var delay = targetDateTime - localTime;

            // Schedule delayed job via Hangfire
            foreach (var notif in savedNotifications)
            {
                BackgroundJob.Schedule<NotificationService>(
                    s => s.SendDirectAsync(notif.Id),
                    delay
                );
            }
        }
        else
        {
            // Send immediately
            foreach (var notif in savedNotifications)
            {
                await DispatchToChannelAsync(notif, user);
            }
        }
    }

    public async Task SendBulkAsync(SendBulkNotificationDto dto)
    {
        foreach (var userId in dto.UserIds)
        {
            var sendDto = new SendNotificationDto
            {
                UserId = userId,
                Type = dto.Type,
                Title = dto.Title,
                Body = dto.Body,
                Data = dto.Data
            };
            // Hangfire queue or call directly
            await SendAsync(sendDto);
        }
    }

    [Queue("default")]
    public async Task SendDirectAsync(Guid notificationId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null) return;

        var user = await _dbContext.Users
            .Include(u => u.DeviceTokens)
            .FirstOrDefaultAsync(u => u.Id == notification.UserId);

        if (user == null) return;

        await DispatchToChannelAsync(notification, user);
    }

    private async Task DispatchToChannelAsync(Notification notification, User user)
    {
        var channel = _channels.FirstOrDefault(c => c.Name == notification.Channel);
        if (channel != null)
        {
            try
            {
                await channel.SendAsync(notification, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching notification {NotificationId} through channel {ChannelName}",
                    notification.Id, channel.Name);
            }
        }
    }

    public async Task<ApiResponse<PageResponse<NotificationDto>>> GetMyNotificationsAsync(
        Guid userId, bool? unreadOnly, int page, int pageSize)
    {
        var query = _dbContext.Notifications
            .Where(n => n.UserId == userId && n.DeletedAt == null);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                Data = n.Data,
                Channel = n.Channel,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                SentAt = n.SentAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var pageResponse = new PageResponse<NotificationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
        return ApiResponse<PageResponse<NotificationDto>>.Ok(pageResponse);
    }

    public async Task<ApiResponse<object>> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.DeletedAt == null);

        if (notification == null)
        {
            return ApiResponse<object>.Fail("Notification not found", ErrorCode.NOTIFICATION_NOT_FOUND);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return ApiResponse<object>.Ok(null!);
    }

    public async Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        if (unread.Any())
        {
            await _dbContext.SaveChangesAsync();
        }

        return ApiResponse<object>.Ok(null!);
    }

    public async Task<ApiResponse<object>> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.DeletedAt == null);

        if (notification == null)
        {
            return ApiResponse<object>.Fail("Notification not found", ErrorCode.NOTIFICATION_NOT_FOUND);
        }

        notification.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!);
    }

    public async Task<ApiResponse<UnreadCountDto>> GetUnreadCountAsync(Guid userId)
    {
        var count = await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null);

        return ApiResponse<UnreadCountDto>.Ok(new UnreadCountDto { UnreadCount = count });
    }

    public async Task<ApiResponse<NotificationSettingDto>> GetSettingsAsync(Guid userId)
    {
        var settings = await _dbContext.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new NotificationSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyCheckin = true,
                StreakReminder = true,
                GroupActivity = true,
                ChallengeUpdates = true,
                Promotions = true,
                PushEnabled = true,
                EmailEnabled = true,
                InAppEnabled = true,
                QuietFrom = new TimeOnly(22, 0),
                QuietUntil = new TimeOnly(7, 0),
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.NotificationSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
        }

        return ApiResponse<NotificationSettingDto>.Ok(MapToSettingDto(settings));
    }

    public async Task<ApiResponse<NotificationSettingDto>> UpdateSettingsAsync(UpdateNotificationSettingDto dto, Guid userId)
    {
        var settings = await _dbContext.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new NotificationSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyCheckin = true,
                StreakReminder = true,
                GroupActivity = true,
                ChallengeUpdates = true,
                Promotions = true,
                PushEnabled = true,
                EmailEnabled = true,
                InAppEnabled = true,
                QuietFrom = new TimeOnly(22, 0),
                QuietUntil = new TimeOnly(7, 0),
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.NotificationSettings.Add(settings);
        }

        if (dto.DailyCheckin.HasValue) settings.DailyCheckin = dto.DailyCheckin.Value;
        if (dto.StreakReminder.HasValue) settings.StreakReminder = dto.StreakReminder.Value;
        if (dto.GroupActivity.HasValue) settings.GroupActivity = dto.GroupActivity.Value;
        if (dto.ChallengeUpdates.HasValue) settings.ChallengeUpdates = dto.ChallengeUpdates.Value;
        if (dto.Promotions.HasValue) settings.Promotions = dto.Promotions.Value;
        if (dto.PushEnabled.HasValue) settings.PushEnabled = dto.PushEnabled.Value;
        if (dto.EmailEnabled.HasValue) settings.EmailEnabled = dto.EmailEnabled.Value;
        if (dto.InAppEnabled.HasValue) settings.InAppEnabled = dto.InAppEnabled.Value;
        if (dto.QuietFrom.HasValue) settings.QuietFrom = dto.QuietFrom.Value;
        if (dto.QuietUntil.HasValue) settings.QuietUntil = dto.QuietUntil.Value;

        settings.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ApiResponse<NotificationSettingDto>.Ok(MapToSettingDto(settings));
    }

    public async Task<ApiResponse<object>> RegisterDeviceTokenAsync(RegisterDeviceTokenDto dto, Guid userId)
    {
        var token = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == dto.Token);

        if (token != null)
        {
            if (!token.IsActive)
            {
                token.IsActive = true;
                token.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            return ApiResponse<object>.Ok(null!);
        }

        // Check if token exists for another user, deactivate or delete it
        var otherToken = await _dbContext.DeviceTokens
            .Where(t => t.Token == dto.Token)
            .ToListAsync();
        if (otherToken.Any())
        {
            _dbContext.DeviceTokens.RemoveRange(otherToken);
        }

        var newDeviceToken = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = dto.Token,
            Platform = dto.Platform,
            DeviceName = dto.DeviceName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.DeviceTokens.Add(newDeviceToken);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!);
    }

    public async Task<ApiResponse<object>> RemoveDeviceTokenAsync(string token, Guid userId)
    {
        var devToken = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId);

        if (devToken != null)
        {
            _dbContext.DeviceTokens.Remove(devToken);
            await _dbContext.SaveChangesAsync();
        }

        return ApiResponse<object>.Ok(null!);
    }

    private NotificationSettingDto MapToSettingDto(NotificationSetting s)
    {
        return new NotificationSettingDto
        {
            Id = s.Id,
            UserId = s.UserId,
            DailyCheckin = s.DailyCheckin,
            StreakReminder = s.StreakReminder,
            GroupActivity = s.GroupActivity,
            ChallengeUpdates = s.ChallengeUpdates,
            Promotions = s.Promotions,
            PushEnabled = s.PushEnabled,
            EmailEnabled = s.EmailEnabled,
            InAppEnabled = s.InAppEnabled,
            QuietFrom = s.QuietFrom,
            QuietUntil = s.QuietUntil,
            UpdatedAt = s.UpdatedAt
        };
    }
}
