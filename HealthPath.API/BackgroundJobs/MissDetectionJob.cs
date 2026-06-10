using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public class MissDetectionJob : IMissDetectionJob
{
    private readonly HealthpathDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<MissDetectionJob> _logger;

    public MissDetectionJob(
        HealthpathDbContext context,
        INotificationService notifications,
        ILogger<MissDetectionJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executing MissDetectionJob...");

        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var startOfDayVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfDayVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 23, 59, 59, DateTimeKind.Unspecified);

        var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(startOfDayVn, tz);
        var endOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(endOfDayVn, tz);

        var missedRoutines = await _context.UserRoutines
            .Where(ur => ur.Status == "pending" &&
                         ur.ScheduledAt >= startOfDayUtc &&
                         ur.ScheduledAt <= endOfDayUtc &&
                         ur.DeletedAt == null)
            .ToListAsync();

        if (!missedRoutines.Any())
        {
            _logger.LogInformation("No missed routines found for today.");
            return;
        }

        foreach (var routine in missedRoutines)
        {
            routine.Status = "failed";
            routine.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} routines as failed (missed).", missedRoutines.Count);

        var byUser = missedRoutines.GroupBy(r => r.UserId);
        foreach (var group in byUser)
        {
            var count = group.Count();
            await _notifications.SendAsync(new SendNotificationDto
            {
                UserId = group.Key,
                Type = "streak_alert",
                Title = "Streak có thể bị gián đoạn 🔥",
                Body = count == 1
                    ? "Hôm nay bạn còn 1 thói quen chưa hoàn thành."
                    : $"Hôm nay bạn còn {count} thói quen chưa hoàn thành.",
                Data = "{\"screen\":\"home\"}"
            });
        }
    }
}
