using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.API.Services.Channels;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationChannel> _mockInAppChannel;
    private readonly Mock<INotificationChannel> _mockPushChannel;
    private readonly Mock<INotificationChannel> _mockEmailChannel;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly List<INotificationChannel> _channels;

    public NotificationServiceTests()
    {
        _mockInAppChannel = new Mock<INotificationChannel>();
        _mockInAppChannel.Setup(c => c.Name).Returns("in_app");

        _mockPushChannel = new Mock<INotificationChannel>();
        _mockPushChannel.Setup(c => c.Name).Returns("push");

        _mockEmailChannel = new Mock<INotificationChannel>();
        _mockEmailChannel.Setup(c => c.Name).Returns("email");

        _mockLogger = new Mock<ILogger<NotificationService>>();

        _channels = new List<INotificationChannel>
        {
            _mockInAppChannel.Object,
            _mockPushChannel.Object,
            _mockEmailChannel.Object
        };
    }

    [Fact]
    public async Task GetSettingsAsync_NoExistingSettings_CreatesAndReturnsDefaultSettings()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);

        // Act
        var result = await service.GetSettingsAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.InAppEnabled.Should().BeTrue();
        result.Data.PushEnabled.Should().BeTrue();
        result.Data.EmailEnabled.Should().BeTrue();

        var dbSetting = context.NotificationSettings.FirstOrDefault(s => s.UserId == userId);
        dbSetting.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesValuesCorrectly()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);
        var updateDto = new UpdateNotificationSettingDto
        {
            InAppEnabled = false,
            PushEnabled = false,
            QuietFrom = new TimeOnly(23, 30),
            QuietUntil = new TimeOnly(6, 0)
        };

        // Act
        var result = await service.UpdateSettingsAsync(updateDto, userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.InAppEnabled.Should().BeFalse();
        result.Data.PushEnabled.Should().BeFalse();
        result.Data.EmailEnabled.Should().BeTrue(); // Unchanged
        result.Data.QuietFrom.Should().Be(new TimeOnly(23, 30));
        result.Data.QuietUntil.Should().Be(new TimeOnly(6, 0));
    }

    [Fact]
    public async Task RegisterDeviceTokenAsync_NewToken_RegistersSuccessfully()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);
        var dto = new RegisterDeviceTokenDto
        {
            Token = "fcm_token_123",
            Platform = "android",
            DeviceName = "Pixel 6"
        };

        // Act
        var result = await service.RegisterDeviceTokenAsync(dto, userId);

        // Assert
        result.Success.Should().BeTrue();
        var token = context.DeviceTokens.FirstOrDefault(t => t.UserId == userId && t.Token == "fcm_token_123");
        token.Should().NotBeNull();
        token!.Platform.Should().Be("android");
        token.DeviceName.Should().Be("Pixel 6");
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDeviceTokenAsync_DeletesSuccessfully()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        var deviceToken = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "token_to_remove",
            Platform = "ios",
            IsActive = true
        };
        context.DeviceTokens.Add(deviceToken);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);

        // Act
        var result = await service.RemoveDeviceTokenAsync("token_to_remove", userId);

        // Assert
        result.Success.Should().BeTrue();
        context.DeviceTokens.Any(t => t.Token == "token_to_remove").Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_UnreadNotification_MarksAsRead()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        context.Notifications.Add(new Notification
        {
            Id = notifId,
            UserId = userId,
            Title = "Title",
            Body = "Body",
            Type = "streak_alert",
            Data = "{}",
            Channel = "in_app",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);

        // Act
        var result = await service.MarkAsReadAsync(notifId, userId);

        // Assert
        result.Success.Should().BeTrue();
        var notif = context.Notifications.First(n => n.Id == notifId);
        notif.IsRead.Should().BeTrue();
        notif.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteNotificationAsync_SoftDeletesNotification()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        });
        context.Notifications.Add(new Notification
        {
            Id = notifId,
            UserId = userId,
            Title = "Title",
            Body = "Body",
            Type = "streak_alert",
            Data = "{}",
            Channel = "in_app",
            IsRead = false,
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);

        // Act
        var result = await service.DeleteNotificationAsync(notifId, userId);

        // Assert
        result.Success.Should().BeTrue();
        var notif = context.Notifications.First(n => n.Id == notifId);
        notif.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_RoutesToEnabledChannels()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@healthpath.vn",
            PasswordHash = "hashed"
        };
        context.Users.Add(user);
        
        var setting = new NotificationSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DailyCheckin = true,
            StreakReminder = true,
            GroupActivity = true,
            ChallengeUpdates = true,
            Promotions = true,
            InAppEnabled = true,
            PushEnabled = false,
            EmailEnabled = true,
            QuietFrom = null,
            QuietUntil = null
        };
        context.NotificationSettings.Add(setting);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _channels, _mockLogger.Object);
        var dto = new SendNotificationDto
        {
            UserId = userId,
            Type = "streak_alert",
            Title = "New Streak!",
            Body = "Keep going!"
        };

        // Act
        await service.SendAsync(dto);

        // Assert
        var savedNotifs = context.Notifications.Where(n => n.UserId == userId).ToList();
        savedNotifs.Should().HaveCount(2); // in_app and email
        savedNotifs.Any(n => n.Channel == "in_app").Should().BeTrue();
        savedNotifs.Any(n => n.Channel == "email").Should().BeTrue();
        savedNotifs.Any(n => n.Channel == "push").Should().BeFalse();
        
        _mockInAppChannel.Verify(c => c.SendAsync(It.IsAny<Notification>(), It.IsAny<User>()), Times.Once);
        _mockEmailChannel.Verify(c => c.SendAsync(It.IsAny<Notification>(), It.IsAny<User>()), Times.Once);
        _mockPushChannel.Verify(c => c.SendAsync(It.IsAny<Notification>(), It.IsAny<User>()), Times.Never);
    }
}
