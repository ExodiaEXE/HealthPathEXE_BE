using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Xunit;

namespace HealthPath.Tests.Services
{
    public class UserRoutineServiceTests
    {
        [Fact]
        public async Task ScheduleRoutineAsync_Valid_ReturnsScheduledRoutine()
        {
            using var context = DbContextFactory.Create();
            var service = new UserRoutineService(context);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Morning Walk",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = false
            });
            await context.SaveChangesAsync();

            var dto = new CreateUserRoutineDto { RoutineId = routineId, ScheduledAt = DateTime.UtcNow.AddDays(1) };

            // Act
            var result = await service.ScheduleRoutineAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be("pending");
            result.Data.ScheduledAt.Should().Be(dto.ScheduledAt);

            context.UserRoutines.Should().ContainSingle();
        }

        [Fact]
        public async Task ScheduleRoutineAsync_PremiumRoutine_WithoutPremiumUser_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var service = new UserRoutineService(context);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Premium Yoga",
                Category = "yoga",
                Difficulty = "medium",
                IsPremium = true
            });
            // Assume user does NOT have active premium subscription here
            await context.SaveChangesAsync();

            var dto = new CreateUserRoutineDto { RoutineId = routineId };

            // Act
            var result = await service.ScheduleRoutineAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.PREMIUM_REQUIRED);
        }

        [Fact]
        public async Task StartRoutineAsync_PendingStatus_ChangesToInProgress()
        {
            using var context = DbContextFactory.Create();
            var service = new UserRoutineService(context);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();
            var userRoutineId = Guid.NewGuid();

            context.UserRoutines.Add(new UserRoutine
            {
                Id = userRoutineId,
                UserId = userId,
                RoutineId = routineId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartRoutineAsync(userRoutineId, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Status.Should().Be("in_progress");
            result.Data.StartedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteRoutineAsync_InProgressStatus_ChangesToCompletedAndCalculatesScore()
        {
            using var context = DbContextFactory.Create();
            var service = new UserRoutineService(context);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();
            var userRoutineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "medium",
                DurationMinutes = 30
            });

            context.UserRoutines.Add(new UserRoutine
            {
                Id = userRoutineId,
                UserId = userId,
                RoutineId = routineId,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var updateDto = new UserRoutineStatusUpdateDto
            {
                Status = "completed",
                ActualDurationMinutes = 30,
                ElapsedSeconds = 1800
            };

            // Act
            var result = await service.CompleteRoutineAsync(userRoutineId, updateDto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Status.Should().Be("completed");
            result.Data.CompletedAt.Should().NotBeNull();
            result.Data.ScoreEarned.Should().BeGreaterThan(0);
            
            // Check UserStats creation/update
            context.UserStats.Should().ContainSingle(s => s.UserId == userId);
        }
    }
}
