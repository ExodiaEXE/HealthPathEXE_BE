using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Options;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IBackgroundJobDispatcher> _mockBackgroundJobs;
        private readonly IOptions<SocialAuthOptions> _socialAuthOptions;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            // Cấu hình JWT Key và Section để dùng sinh token
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(s => s["Key"]).Returns("CaiChiaKhoaExodiaKhoiNghiepNha123456789");
            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(mockJwtSection.Object);

            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("CaiChiaKhoaExodiaKhoiNghiepNha123456789");
            _mockConfiguration.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Development");

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockBackgroundJobs = new Mock<IBackgroundJobDispatcher>();
            _socialAuthOptions = Options.Create(new SocialAuthOptions { AllowMockTokens = true });
        }

        private AuthService CreateService(HealthpathDbContext context) =>
            new(
                context,
                _mockConfiguration.Object,
                _mockHttpClientFactory.Object,
                _mockBackgroundJobs.Object,
                _socialAuthOptions);

        [Fact]
        public async Task SocialLoginAsync_GoogleNewUser_RegistersAndLogsIn()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var service = CreateService(context);

            var dto = new SocialLoginDto
            {
                Token = "mock_google_token_newuser",
                Provider = "google"
            };

            // Act
            var result = await service.SocialLoginAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();

            var userInDb = context.Users.FirstOrDefault(u => u.GoogleId == "google_id_newuser");
            userInDb.Should().NotBeNull();
            userInDb!.Email.Should().Be("newuser@gmail.com");
            userInDb.FullName.Should().Be("Google User newuser");
            userInDb.IsVerified.Should().BeTrue();
        }

        [Fact]
        public async Task SocialLoginAsync_GoogleExistingSocialUser_LogsIn()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Existing User",
                Email = "existing@gmail.com",
                PasswordHash = "hashed",
                GoogleId = "google_id_existing",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new SocialLoginDto
            {
                Token = "mock_google_token_existing",
                Provider = "google"
            };

            // Act
            var result = await service.SocialLoginAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SocialLoginAsync_GoogleMatchesExistingEmail_AutoLinks()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Local User",
                Email = "matchedemail@gmail.com",
                PasswordHash = "hashed",
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new SocialLoginDto
            {
                Token = "mock_google_token_matchedemail",
                Provider = "google"
            };

            // Act
            var result = await service.SocialLoginAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();

            var updatedUser = context.Users.First(u => u.Email == "matchedemail@gmail.com");
            updatedUser.GoogleId.Should().Be("google_id_matchedemail");
            updatedUser.IsVerified.Should().BeTrue();
            updatedUser.EmailVerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task LinkSocialAccountAsync_ValidNewLink_Succeeds()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Auth User",
                Email = "authuser@gmail.com",
                PasswordHash = "hashed",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new SocialLinkDto
            {
                Token = "mock_google_token_linkme",
                Provider = "google"
            };

            // Act
            var result = await service.LinkSocialAccountAsync(userId, dto);

            // Assert
            result.Success.Should().BeTrue();
            user.GoogleId.Should().Be("google_id_linkme");
        }

        [Fact]
        public async Task LinkSocialAccountAsync_AlreadyLinkedToAnother_ReturnsFail()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            var user1 = new User
            {
                Id = userId1,
                FullName = "User One",
                Email = "user1@gmail.com",
                PasswordHash = "hashed",
                GoogleId = "google_id_occupied",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var user2 = new User
            {
                Id = userId2,
                FullName = "User Two",
                Email = "user2@gmail.com",
                PasswordHash = "hashed",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new SocialLinkDto
            {
                Token = "mock_google_token_occupied",
                Provider = "google"
            };

            // Act
            var result = await service.LinkSocialAccountAsync(userId2, dto);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(ErrorCode.EXTERNAL_ACCOUNT_ALREADY_LINKED.ToString());
            user2.GoogleId.Should().BeNull();
        }

        [Fact]
        public async Task UnlinkSocialAccountAsync_LastRemainingProvider_ReturnsFail()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Social Only User",
                Email = "social@gmail.com",
                PasswordHash = "", // Không có password
                GoogleId = "google_id_only",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UnlinkSocialAccountAsync(userId, "google");

            // Assert
            result.Success.Should().BeFalse();
            user.GoogleId.Should().Be("google_id_only"); // Vẫn giữ nguyên để tránh khóa acc
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidCredentials_UpdatesPassword()
        {
            var plainOld = "OldPass@1";
            var plainNew = "NewPass@2";
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Password User",
                Email = "password@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainOld),
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ChangePasswordAsync(userId, new ChangePasswordDto
            {
                CurrentPassword = plainOld,
                NewPassword = plainNew
            });

            result.Success.Should().BeTrue();
            var userInDb = context.Users.First(u => u.Id == userId);
            BCrypt.Net.BCrypt.Verify(plainNew, userInDb.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_Fails()
        {
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Password User",
                Email = "password2@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@1"),
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ChangePasswordAsync(userId, new ChangePasswordDto
            {
                CurrentPassword = "WrongPass@1",
                NewPassword = "NewPass@2"
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Mật khẩu hiện tại không đúng");
        }

        [Fact]
        public async Task UnlinkSocialAccountAsync_HasPassword_Succeeds()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Normal User",
                Email = "normal@gmail.com",
                PasswordHash = "hashed_password",
                GoogleId = "google_id_to_remove",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UnlinkSocialAccountAsync(userId, "google");

            // Assert
            result.Success.Should().BeTrue();
            user.GoogleId.Should().BeNull();
        }
    }
}
