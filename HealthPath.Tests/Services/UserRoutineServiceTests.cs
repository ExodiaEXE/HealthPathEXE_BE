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
using Moq;

namespace HealthPath.Tests.Services
{
    public class UserRoutineServiceTests
    {
        [Fact]
        public async Task ScheduleRoutineAsync_Valid_ReturnsScheduledRoutine()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

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
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

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
            result.ErrorCode.Should().Be(ErrorCode.PREMIUM_REQUIRED.ToString());
        }

        [Fact]
        public async Task StartRoutineAsync_PendingStatus_ChangesToInProgress()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

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
        public async Task CompleteRoutineAsync_InProgressStatus_ChangesToCompletedAndTriggersGamification()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

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
            
            // Verify Gamification logic was triggered
            mockGamificationService.Verify(g => g.ProcessCompletionAsync(userRoutineId, userId), Times.Once);
        }

        [Fact]
        public async Task CreateRecurringTemplateAsync_Valid_ReturnsSuccess()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = false
            });
            await context.SaveChangesAsync();

            var dto = new CreateRecurringTemplateDto
            {
                RoutineId = routineId,
                DaysOfWeek = new System.Collections.Generic.List<int> { 1, 3, 5 },
                ScheduledTime = "07:30:00"
            };

            // Act
            var result = await service.CreateRecurringTemplateAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.DaysOfWeek.Should().BeEquivalentTo(new[] { 1, 3, 5 });
            result.Data.ScheduledTime.Should().Be("07:30:00");
            result.Data.IsActive.Should().BeTrue();

            context.RecurringTemplates.Should().ContainSingle();
        }

        [Fact]
        public async Task CreateRecurringTemplateAsync_RoutineNotFound_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var dto = new CreateRecurringTemplateDto
            {
                RoutineId = Guid.NewGuid(),
                DaysOfWeek = new System.Collections.Generic.List<int> { 1 },
                ScheduledTime = "07:30:00"
            };

            // Act
            var result = await service.CreateRecurringTemplateAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.ROUTINE_NOT_FOUND.ToString());
        }

        [Fact]
        public async Task CreateRecurringTemplateAsync_PremiumWithoutSub_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = true
            });
            await context.SaveChangesAsync();

            var dto = new CreateRecurringTemplateDto
            {
                RoutineId = routineId,
                DaysOfWeek = new System.Collections.Generic.List<int> { 1 },
                ScheduledTime = "07:30:00"
            };

            // Act
            var result = await service.CreateRecurringTemplateAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.PREMIUM_REQUIRED.ToString());
        }

        [Fact]
        public async Task CreateRecurringTemplateAsync_InvalidTime_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = false
            });
            await context.SaveChangesAsync();

            var dto = new CreateRecurringTemplateDto
            {
                RoutineId = routineId,
                DaysOfWeek = new System.Collections.Generic.List<int> { 1 },
                ScheduledTime = "99:99:99" // Invalid time
            };

            // Act
            var result = await service.CreateRecurringTemplateAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.VALIDATION_ERROR.ToString());
        }

        [Fact]
        public async Task CreateRecurringTemplateAsync_InvalidDays_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = false
            });
            await context.SaveChangesAsync();

            var dto = new CreateRecurringTemplateDto
            {
                RoutineId = routineId,
                DaysOfWeek = new System.Collections.Generic.List<int> { 0, 8 }, // Invalid days
                ScheduledTime = "07:30:00"
            };

            // Act
            var result = await service.CreateRecurringTemplateAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.VALIDATION_ERROR.ToString());
        }

        [Fact]
        public async Task GetMyRecurringTemplatesAsync_ReturnsActiveOwnedTemplates()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.Routines.Add(new Routine
            {
                Id = routineId,
                Title = "Workout",
                Category = "workout",
                Difficulty = "easy",
                IsPremium = false
            });

            // Owned template
            context.RecurringTemplates.Add(new RecurringTemplate
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoutineId = routineId,
                DaysOfWeek = "[1]",
                ScheduledTime = new TimeOnly(7, 30),
                IsActive = true
            });

            // Deleted owned template
            context.RecurringTemplates.Add(new RecurringTemplate
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoutineId = routineId,
                DaysOfWeek = "[2]",
                ScheduledTime = new TimeOnly(8, 30),
                IsActive = false,
                DeletedAt = DateTime.UtcNow
            });

            // Other user's template
            context.RecurringTemplates.Add(new RecurringTemplate
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                RoutineId = routineId,
                DaysOfWeek = "[3]",
                ScheduledTime = new TimeOnly(9, 30),
                IsActive = true
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetMyRecurringTemplatesAsync(userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle();
            result.Data![0].UserId.Should().Be(userId);
            result.Data[0].DaysOfWeek.Should().BeEquivalentTo(new[] { 1 });
        }

        [Fact]
        public async Task DeleteRecurringTemplateAsync_Valid_SoftDeletesTemplate()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var routineId = Guid.NewGuid();

            context.RecurringTemplates.Add(new RecurringTemplate
            {
                Id = templateId,
                UserId = userId,
                RoutineId = routineId,
                DaysOfWeek = "[1]",
                ScheduledTime = new TimeOnly(7, 30),
                IsActive = true
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteRecurringTemplateAsync(templateId, userId);

            // Assert
            result.Success.Should().BeTrue();
            
            var template = context.RecurringTemplates.First();
            template.IsActive.Should().BeFalse();
            template.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteRecurringTemplateAsync_NotFoundOrNotOwned_ReturnsFail()
        {
            using var context = DbContextFactory.Create();
            var mockGamificationService = new Mock<IGamificationService>();
            var service = new UserRoutineService(context, mockGamificationService.Object);

            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            // Act
            var result = await service.DeleteRecurringTemplateAsync(templateId, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.USER_ROUTINE_NOT_FOUND.ToString());
        }
    }
}

