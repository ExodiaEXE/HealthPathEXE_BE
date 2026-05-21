using System;
using System.Collections.Generic;

namespace HealthPath.API.Models.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Data { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendNotificationDto
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!; // routine_reminder, streak_alert, group_activity, challenge_update, promotion
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Data { get; set; } // JSON format
}

public class SendBulkNotificationDto
{
    public List<Guid> UserIds { get; set; } = new();
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Data { get; set; }
}

public class NotificationSettingDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool DailyCheckin { get; set; }
    public bool StreakReminder { get; set; }
    public bool GroupActivity { get; set; }
    public bool ChallengeUpdates { get; set; }
    public bool Promotions { get; set; }
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public TimeOnly? QuietFrom { get; set; }
    public TimeOnly? QuietUntil { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateNotificationSettingDto
{
    public bool? DailyCheckin { get; set; }
    public bool? StreakReminder { get; set; }
    public bool? GroupActivity { get; set; }
    public bool? ChallengeUpdates { get; set; }
    public bool? Promotions { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? InAppEnabled { get; set; }
    public TimeOnly? QuietFrom { get; set; }
    public TimeOnly? QuietUntil { get; set; }
}

public class RegisterDeviceTokenDto
{
    public string Token { get; set; } = null!;
    public string Platform { get; set; } = null!; // android, ios
    public string? DeviceName { get; set; }
}

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}
