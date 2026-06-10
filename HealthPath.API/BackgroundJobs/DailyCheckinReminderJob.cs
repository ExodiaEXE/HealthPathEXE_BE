using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public class DailyCheckinReminderJob : IDailyCheckinReminderJob
{
    private readonly HealthpathDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<DailyCheckinReminderJob> _logger;

    public DailyCheckinReminderJob(
        HealthpathDbContext context,
        INotificationService notifications,
        ILogger<DailyCheckinReminderJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executing DailyCheckinReminderJob...");

        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var startOfDayVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(startOfDayVn, tz);
        var endOfDayUtc = startOfDayUtc.AddDays(1);

        var checkedInUserIds = await _context.MoodCheckins
            .Where(m => m.DeletedAt == null &&
                        m.CheckedAt >= startOfDayUtc &&
                        m.CheckedAt < endOfDayUtc)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync();

        var usersToNotify = await _context.Users
            .Where(u => u.DeletedAt == null && !checkedInUserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var isMorning = nowVn.Hour < 12;
        foreach (var userId in usersToNotify)
        {
            await _notifications.SendAsync(new SendNotificationDto
            {
                UserId = userId,
                Type = "daily_checkin",
                Title = isMorning ? "Chào buổi sáng ☀️" : "Check-in buổi tối 🌙",
                Body = isMorning
                    ? "Ghi nhận tâm trạng và năng lượng để bắt đầu ngày mới."
                    : "Dành vài giây ghi lại cảm xúc hôm nay nhé.",
                Data = "{\"screen\":\"home\"}"
            });
        }

        _logger.LogInformation("DailyCheckinReminderJob sent {Count} reminders.", usersToNotify.Count);
    }
}
