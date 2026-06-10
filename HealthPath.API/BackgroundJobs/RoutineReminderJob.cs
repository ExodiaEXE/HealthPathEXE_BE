using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public class RoutineReminderJob : IRoutineReminderJob
{
    private readonly HealthpathDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<RoutineReminderJob> _logger;

    public RoutineReminderJob(
        HealthpathDbContext context,
        INotificationService notifications,
        ILogger<RoutineReminderJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executing RoutineReminderJob...");

        var nowUtc = DateTime.UtcNow;
        var windowStart = nowUtc.AddMinutes(-15);
        var windowEnd = nowUtc.AddMinutes(15);

        var dueRoutines = await _context.UserRoutines
            .Include(ur => ur.Routine)
            .Where(ur => ur.DeletedAt == null &&
                         ur.Status == "pending" &&
                         ur.ScheduledAt != null &&
                         ur.ScheduledAt >= windowStart &&
                         ur.ScheduledAt <= windowEnd)
            .ToListAsync();

        if (!dueRoutines.Any())
        {
            _logger.LogInformation("RoutineReminderJob: no routines due in window.");
            return;
        }

        var startOfDayUtc = nowUtc.Date;
        var groups = dueRoutines.GroupBy(r => r.UserId);

        foreach (var group in groups)
        {
            var userId = group.Key;
            var routineNames = group
                .Select(r => r.Routine?.Title ?? "Thói quen")
                .Distinct()
                .Take(3)
                .ToList();

            var alreadySent = await _context.Notifications.AnyAsync(n =>
                n.UserId == userId &&
                n.Type == "daily_checkin" &&
                n.DeletedAt == null &&
                n.CreatedAt >= startOfDayUtc &&
                n.Title.Contains("Nhắc thói quen"));

            if (alreadySent) continue;

            var body = routineNames.Count == 1
                ? $"Đến giờ: {routineNames[0]}"
                : $"Bạn có {group.Count()} thói quen cần thực hiện.";

            await _notifications.SendAsync(new SendNotificationDto
            {
                UserId = userId,
                Type = "daily_checkin",
                Title = "Nhắc thói quen ⏰",
                Body = body,
                Data = "{\"screen\":\"home\"}"
            });
        }

        _logger.LogInformation("RoutineReminderJob processed {Count} routines.", dueRoutines.Count);
    }
}
