using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.BackgroundJobs;
using HealthPath.API.Models;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HealthPath.Tests.BackgroundJobs;

public class RecurringRoutineJobTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateRoutines_WhenDayMatches()
    {
        using var context = DbContextFactory.Create();
        var logger = NullLogger<RecurringRoutineJob>.Instance;
        var job = new RecurringRoutineJob(context, logger);

        var userId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        
        // Find what day it is today in SE Asia Standard Time
        var todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        int currentDayOfWeek = (int)todayVn.DayOfWeek == 0 ? 7 : (int)todayVn.DayOfWeek;
        
        context.RecurringTemplates.Add(new RecurringTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoutineId = routineId,
            DaysOfWeek = $"[{currentDayOfWeek}]",
            ScheduledTime = new TimeOnly(8, 0, 0),
            IsActive = true
        });

        // Add another that does not match today
        int otherDay = currentDayOfWeek == 1 ? 2 : 1;
        context.RecurringTemplates.Add(new RecurringTemplate
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoutineId = Guid.NewGuid(),
            DaysOfWeek = $"[{otherDay}]",
            ScheduledTime = new TimeOnly(10, 0, 0),
            IsActive = true
        });

        await context.SaveChangesAsync();

        // Act
        await job.ExecuteAsync();

        // Assert
        var createdRoutines = context.UserRoutines.ToList();
        createdRoutines.Should().HaveCount(1);
        
        var routine = createdRoutines.First();
        routine.UserId.Should().Be(userId);
        routine.RoutineId.Should().Be(routineId);
        routine.Status.Should().Be("pending");
        routine.ScheduledAt.Should().NotBeNull();
    }
}
