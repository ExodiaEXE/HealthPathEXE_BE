using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public class MissDetectionJob : IMissDetectionJob
{
    private readonly HealthpathDbContext _context;
    private readonly ILogger<MissDetectionJob> _logger;

    public MissDetectionJob(HealthpathDbContext context, ILogger<MissDetectionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executing MissDetectionJob...");

        // Get current time in UTC+7 (SE Asia Standard Time)
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        var startOfDayVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfDayVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 23, 59, 59, DateTimeKind.Unspecified);

        var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(startOfDayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        var endOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(endOfDayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        var missedRoutines = await _context.UserRoutines
            .Where(ur => ur.Status == "pending" && 
                         ur.ScheduledAt >= startOfDayUtc && 
                         ur.ScheduledAt <= endOfDayUtc &&
                         ur.DeletedAt == null)
            .ToListAsync();

        if (missedRoutines.Any())
        {
            foreach (var routine in missedRoutines)
            {
                routine.Status = "failed"; // Mark as missed/failed
                routine.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Marked {missedRoutines.Count} routines as failed (missed).");
        }
        else
        {
            _logger.LogInformation("No missed routines found for today.");
        }
    }
}
