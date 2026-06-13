using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.BackgroundJobs;
using HealthPath.API.Models;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HealthPath.Tests.BackgroundJobs;

public class MissDetectionJobTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldMarkPendingTodayRoutinesAsFailed()
    {
        using var context = DbContextFactory.Create();
        var logger = NullLogger<MissDetectionJob>.Instance;
        var notifications = new Mock<INotificationService>();
        var job = new MissDetectionJob(context, notifications.Object, logger);

        // Get current time in UTC+7 (SE Asia Standard Time)
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // A time today at 12:00 PM
        var scheduledVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 12, 0, 0, DateTimeKind.Unspecified);
        var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // A time yesterday
        var scheduledYesterdayVn = scheduledVn.AddDays(-1);
        var scheduledYesterdayUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledYesterdayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        var routineToday = new UserRoutine
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoutineId = Guid.NewGuid(),
            Status = "pending",
            ScheduledAt = scheduledUtc, // Should match today
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var routineYesterday = new UserRoutine
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoutineId = Guid.NewGuid(),
            Status = "pending",
            ScheduledAt = scheduledYesterdayUtc, // Should not match today's date range
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var routineCompletedToday = new UserRoutine
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoutineId = Guid.NewGuid(),
            Status = "completed",
            ScheduledAt = scheduledUtc, // Match today, but status completed
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.UserRoutines.AddRange(routineToday, routineYesterday, routineCompletedToday);
        await context.SaveChangesAsync();

        // Act
        await job.ExecuteAsync();

        // Assert
        var updatedRoutineToday = await context.UserRoutines.FindAsync(routineToday.Id);
        updatedRoutineToday!.Status.Should().Be("failed");

        var updatedRoutineYesterday = await context.UserRoutines.FindAsync(routineYesterday.Id);
        updatedRoutineYesterday!.Status.Should().Be("pending");

        var updatedRoutineCompleted = await context.UserRoutines.FindAsync(routineCompletedToday.Id);
        updatedRoutineCompleted!.Status.Should().Be("completed");
    }
}
