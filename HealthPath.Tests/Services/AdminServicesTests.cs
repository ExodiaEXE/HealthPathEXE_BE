using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HealthPath.Tests.Services;

public class AdminServicesTests
{
    private readonly IConfiguration _configuration;

    public AdminServicesTests()
    {
        // Simple in-memory configuration for JWT secret
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Jwt:Key", "CaiChiaKhoaExodiaKhoiNghiepNha123456789" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();
    }

    [Fact]
    public async Task AdminAuth_Login_WithSeededAdmin_Succeeds()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        
        // Seed default Admin
        var adminPassword = "adminPassword123";
        context.Admins.Add(new Admin
        {
            Id = Guid.NewGuid(),
            Username = "superadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FullName = "Super Administrator",
            Role = "SuperAdmin",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var authService = new AdminAuthService(context, _configuration);

        // Act
        var result = await authService.LoginAsync(new AdminLoginDto
        {
            Username = "superadmin",
            Password = adminPassword
        });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("superadmin");
        result.Data.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AdminAuth_CreateAdmin_DuplicateUsername_ReturnsFail()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        context.Admins.Add(new Admin
        {
            Id = Guid.NewGuid(),
            Username = "existingadmin",
            PasswordHash = "hashedpass",
            FullName = "Existing Admin",
            Role = "Moderator",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var authService = new AdminAuthService(context, _configuration);

        // Act
        var result = await authService.CreateAdminAsync(new CreateAdminDto
        {
            Username = "existingadmin",
            Password = "newpassword123",
            FullName = "New Name",
            Role = "Moderator"
        });

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.EMAIL_TAKEN.ToString());
    }

    [Fact]
    public async Task AdminUser_ToggleUserActive_TogglesCorrectly()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Standard User",
            Email = "user@healthpath.vn",
            PasswordHash = "hash",
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var userService = new AdminUserService(context);

        // Act & Assert 1: Toggle from active to inactive
        var result1 = await userService.ToggleUserActiveAsync(userId);
        result1.Success.Should().BeTrue();
        result1.Data.Should().BeFalse(); // Now blocked

        var dbUser1 = context.Users.First(u => u.Id == userId);
        dbUser1.IsActive.Should().BeFalse();

        // Act & Assert 2: Toggle back to active
        var result2 = await userService.ToggleUserActiveAsync(userId);
        result2.Success.Should().BeTrue();
        result2.Data.Should().BeTrue(); // Now unlocked

        var dbUser2 = context.Users.First(u => u.Id == userId);
        dbUser2.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AdminDashboard_GetStats_CompilesStatsCorrectly()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        
        // Seed users
        context.Users.Add(new User { Id = Guid.NewGuid(), FullName = "U1", Email = "u1@test.com", PasswordHash = "h", IsActive = true });
        context.Users.Add(new User { Id = Guid.NewGuid(), FullName = "U2", Email = "u2@test.com", PasswordHash = "h", IsActive = true });
        
        // Seed active subscriptions
        var planId = Guid.NewGuid();
        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Vip Monthly",
            Code = "vip",
            IsActive = true,
            Currency = "VND",
            Features = "[]"
        });

        context.UserSubscriptions.Add(new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PlanId = planId,
            Status = "active",
            BillingCycle = "monthly",
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        });

        // Seed transactions
        context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PlanId = planId,
            Platform = "GooglePlay",
            PlatformTransactionId = "gplay_1",
            PurchaseToken = "t",
            Status = "Success",
            Amount = 50000,
            Currency = "VND",
            PurchasedAt = DateTime.UtcNow
        });

        context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PlanId = planId,
            Platform = "AppStore",
            PlatformTransactionId = "apple_1",
            PurchaseToken = "t",
            Status = "Success",
            Amount = 100000,
            Currency = "VND",
            PurchasedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var dashboardService = new AdminDashboardService(context);

        // Act
        var result = await dashboardService.GetDashboardStatsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalUsers.Should().Be(2);
        result.Data.TotalPremiumUsers.Should().Be(1);
        result.Data.ConversionRate.Should().Be(50.0);
        result.Data.TotalRevenue.Should().Be(150000);
        result.Data.PlatformBreakdown.Should().HaveCount(2);
        result.Data.MonthlyRevenueChart.Should().HaveCount(6);
    }
}
