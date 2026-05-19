using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services
{
    public class RoutineServiceTests
    {
        [Fact]
        public async Task GetRoutinesAsync_ReturnsCorrectPageSize()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            for (int i = 0; i < 15; i++)
            {
                context.Routines.Add(new Routine
                {
                    Id = Guid.NewGuid(),
                    Title = $"Routine {i}",
                    Category = "yoga",
                    Difficulty = "easy"
                });
            }
            await context.SaveChangesAsync();

            var service = new RoutineService(context);

            // Act
            var result = await service.GetRoutinesAsync(category: null, difficulty: null, page: 1, pageSize: 10);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(10);
            result.Data.TotalItems.Should().Be(15);
            result.Data.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetRoutineByIdAsync_ExistingId_ReturnsRoutine()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var id = Guid.NewGuid();
            context.Routines.Add(new Routine
            {
                Id = id,
                Title = "Test Routine",
                Category = "meditation",
                Difficulty = "easy"
            });
            await context.SaveChangesAsync();

            var service = new RoutineService(context);

            // Act
            var result = await service.GetRoutineByIdAsync(id);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Test Routine");
        }

        [Fact]
        public async Task GetRoutineByIdAsync_NonExistingId_ReturnsFail()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var service = new RoutineService(context);

            // Act
            var result = await service.GetRoutineByIdAsync(Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.ROUTINE_NOT_FOUND);
        }

        [Fact]
        public async Task CreateRoutineAsync_ValidDto_ReturnsSuccess()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var service = new RoutineService(context);
            var currentUserId = Guid.NewGuid();
            var dto = new CreateRoutineDto
            {
                Title = "New Routine",
                Category = "workout",
                Difficulty = "medium",
                DurationMinutes = 20,
                IsPremium = false
            };

            // Act
            var result = await service.CreateRoutineAsync(dto, currentUserId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("New Routine");

            var dbRoutine = context.Routines.Single();
            dbRoutine.Title.Should().Be("New Routine");
            dbRoutine.CreatedBy.Should().Be(currentUserId);
        }
    }
}
