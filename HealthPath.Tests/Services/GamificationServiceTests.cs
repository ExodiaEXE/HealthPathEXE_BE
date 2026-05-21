using System;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Models;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.Tests.Services;

public class GamificationServiceTests
{
    [Fact]
    public async Task ProcessCompletion_FirstCompletionToday_StreakStaysAt1()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);

        var userId = Guid.NewGuid();
        var userRoutineId = Guid.NewGuid();

        context.UserRoutines.Add(new UserRoutine
        {
            Id = userRoutineId,
            UserId = userId,
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.ProcessCompletionAsync(userRoutineId, userId);

        var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        stats.Should().NotBeNull();
        stats!.StreakCurrent.Should().Be(1);
        stats.StreakBest.Should().Be(1);
        stats.StreakUpdatedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task ProcessCompletion_ConsecutiveDay_IncrementsStreak()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);

        var userId = Guid.NewGuid();
        var userRoutineId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        context.UserStats.Add(new UserStats
        {
            UserId = userId,
            StreakCurrent = 5,
            StreakBest = 5,
            StreakUpdatedDate = yesterday
        });

        context.UserRoutines.Add(new UserRoutine
        {
            Id = userRoutineId,
            UserId = userId,
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.ProcessCompletionAsync(userRoutineId, userId);

        var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        stats!.StreakCurrent.Should().Be(6);
        stats.StreakBest.Should().Be(6);
        stats.StreakUpdatedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task ProcessCompletion_SameDay_StreakUnchanged()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);

        var userId = Guid.NewGuid();
        var userRoutineId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.UserStats.Add(new UserStats
        {
            UserId = userId,
            StreakCurrent = 5,
            StreakBest = 5,
            StreakUpdatedDate = today
        });

        context.UserRoutines.Add(new UserRoutine
        {
            Id = userRoutineId,
            UserId = userId,
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.ProcessCompletionAsync(userRoutineId, userId);

        var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        stats!.StreakCurrent.Should().Be(5); // unchanged
        stats.StreakBest.Should().Be(5);
        stats.StreakUpdatedDate.Should().Be(today);
    }

    [Fact]
    public async Task ProcessCompletion_GapDay_ResetsStreak()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);

        var userId = Guid.NewGuid();
        var userRoutineId = Guid.NewGuid();
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);

        context.UserStats.Add(new UserStats
        {
            UserId = userId,
            StreakCurrent = 5,
            StreakBest = 5,
            StreakUpdatedDate = twoDaysAgo
        });

        context.UserRoutines.Add(new UserRoutine
        {
            Id = userRoutineId,
            UserId = userId,
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.ProcessCompletionAsync(userRoutineId, userId);

        var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        stats!.StreakCurrent.Should().Be(1); // reset
        stats.StreakBest.Should().Be(5);     // best remains
        stats.StreakUpdatedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task ProcessCompletion_NewBestStreak_UpdatesStreakBest()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);

        var userId = Guid.NewGuid();
        var userRoutineId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        context.UserStats.Add(new UserStats
        {
            UserId = userId,
            StreakCurrent = 5, // it will become 6
            StreakBest = 5,    // so best will also update to 6
            StreakUpdatedDate = yesterday
        });

        context.UserRoutines.Add(new UserRoutine
        {
            Id = userRoutineId,
            UserId = userId,
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.ProcessCompletionAsync(userRoutineId, userId);

        var stats = await context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        stats!.StreakCurrent.Should().Be(6);
        stats.StreakBest.Should().Be(6); // updated
    }

    [Fact]
    public async Task GetUserStatsAsync_ShouldReturnDefaultValues_WhenNoStatsExist()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetUserStatsAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StreakCurrent.Should().Be(0);
        result.Data.StreakBest.Should().Be(0);
        result.Data.StreakUpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task GetUserStatsAsync_ShouldReturnCorrectStats_WhenStatsExist()
    {
        using var context = DbContextFactory.Create();
        var service = new GamificationService(context);
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.UserStats.Add(new UserStats
        {
            UserId = userId,
            StreakCurrent = 7,
            StreakBest = 12,
            StreakUpdatedDate = today
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserStatsAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StreakCurrent.Should().Be(7);
        result.Data.StreakBest.Should().Be(12);
        result.Data.StreakUpdatedDate.Should().Be(today);
    }
}
