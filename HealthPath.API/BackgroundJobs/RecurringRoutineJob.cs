using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public class RecurringRoutineJob : IRecurringRoutineJob
{
    private readonly HealthpathDbContext _context;
    private readonly ILogger<RecurringRoutineJob> _logger;

    public RecurringRoutineJob(HealthpathDbContext context, ILogger<RecurringRoutineJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Executing RecurringRoutineJob...");

        // Get current time in UTC+7 (SE Asia Standard Time)
        var todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Map C# DayOfWeek to 1=Mon...7=Sun
        int currentDayOfWeek = (int)todayVn.DayOfWeek == 0 ? 7 : (int)todayVn.DayOfWeek;

        var activeTemplates = await _context.RecurringTemplates
            .Where(t => t.IsActive && t.DeletedAt == null)
            .ToListAsync();

        var routinesToInsert = new List<UserRoutine>();

        foreach (var template in activeTemplates)
        {
            try
            {
                var days = JsonSerializer.Deserialize<List<int>>(template.DaysOfWeek);
                if (days != null && days.Contains(currentDayOfWeek))
                {
                    // Create scheduled time for today
                    var scheduledAt = new DateTime(
                        todayVn.Year, todayVn.Month, todayVn.Day,
                        template.ScheduledTime.Hour, template.ScheduledTime.Minute, template.ScheduledTime.Second,
                        DateTimeKind.Unspecified);

                    // Convert back to UTC for saving to DB
                    var scheduledAtUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledAt, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                    routinesToInsert.Add(new UserRoutine
                    {
                        UserId = template.UserId,
                        RoutineId = template.RoutineId,
                        Status = "pending",
                        ScheduledAt = scheduledAtUtc,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process template {template.Id}");
            }
        }

        if (routinesToInsert.Any())
        {
            _context.UserRoutines.AddRange(routinesToInsert);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Scheduled {routinesToInsert.Count} routines for today.");
        }
        else
        {
            _logger.LogInformation("No routines scheduled for today.");
        }
    }
}
