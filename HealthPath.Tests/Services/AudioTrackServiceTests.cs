using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services
{
    public class AudioTrackServiceTests
    {
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly Mock<ILogger<AudioTrackService>> _loggerMock;

        public AudioTrackServiceTests()
        {
            _fileStorageMock = new Mock<IFileStorageService>();
            _loggerMock = new Mock<ILogger<AudioTrackService>>();

            // Setup default mock behavior for presigned URL generation
            _fileStorageMock.Setup(f => f.GeneratePresignedDownloadUrlAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((string key, int expires) => $"https://mock-r2-download.com/{key}?expires={expires}");
        }

        // --- Helper to setup admin role in DB ---
        private async Task SetupAdminRoleAsync(HealthpathDbContext dbContext, Guid adminUserId)
        {
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "admin"
            };
            dbContext.Roles.Add(adminRole);

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                RoleId = adminRole.Id
            };
            dbContext.UserRoles.Add(userRole);
            await dbContext.SaveChangesAsync();
        }

        // --- Helper to setup premium subscription in DB ---
        private async Task SetupPremiumSubscriptionAsync(HealthpathDbContext dbContext, Guid userId)
        {
            var sub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "active",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
            dbContext.UserSubscriptions.Add(sub);
            await dbContext.SaveChangesAsync();
        }

        // --- Helper to setup dummy track and category ---
        private async Task<(Guid categoryId, Guid trackId)> SetupTrackAndCategoryAsync(HealthpathDbContext dbContext, string categoryName = "meditation", bool isPremium = false)
        {
            var category = new AudioCategory
            {
                Id = Guid.NewGuid(),
                Name = categoryName,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.AudioCategories.Add(category);

            var track = new AudioTrack
            {
                Id = Guid.NewGuid(),
                Title = "Morning Calm",
                Artist = "Zen Master",
                Studio = "Peace Studios",
                CategoryId = category.Id,
                DurationSeconds = 300,
                FileUrl = "audio/tracks/calm.mp3",
                CoverUrl = "https://public-r2.com/covers/calm.webp",
                IsPremium = isPremium,
                IsActive = true,
                PlayCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.AudioTracks.Add(track);
            await dbContext.SaveChangesAsync();

            return (category.Id, track.Id);
        }

        // ==========================================
        // --- 1. GetTracks_ReturnsPagedResults ---
        // ==========================================
        [Fact]
        public async Task GetTracks_ReturnsPagedResults()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var category = new AudioCategory { Id = Guid.NewGuid(), Name = "sleep", IsActive = true };
            context.AudioCategories.Add(category);

            for (int i = 0; i < 5; i++)
            {
                context.AudioTracks.Add(new AudioTrack
                {
                    Id = Guid.NewGuid(),
                    Title = $"Sleep Track {i}",
                    CategoryId = category.Id,
                    DurationSeconds = 120,
                    FileUrl = $"key_{i}",
                    IsActive = true
                });
            }
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetTracksAsync(category: null, search: null, isPremium: null, sortBy: "newest", page: 1, pageSize: 3, currentUserId: null);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(3);
            response.Data.TotalItems.Should().Be(5);
            response.Data.TotalPages.Should().Be(2);
        }

        // ==========================================
        // --- 2. GetTracks_FilterByCategory -------
        // ==========================================
        [Fact]
        public async Task GetTracks_FilterByCategory()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var catMeditation = new AudioCategory { Id = Guid.NewGuid(), Name = "meditation", IsActive = true };
            var catSleep = new AudioCategory { Id = Guid.NewGuid(), Name = "sleep", IsActive = true };
            context.AudioCategories.AddRange(catMeditation, catSleep);

            context.AudioTracks.Add(new AudioTrack { Id = Guid.NewGuid(), Title = "Meditation Sound", CategoryId = catMeditation.Id, FileUrl = "med_key", IsActive = true });
            context.AudioTracks.Add(new AudioTrack { Id = Guid.NewGuid(), Title = "Sleep Sound", CategoryId = catSleep.Id, FileUrl = "sleep_key", IsActive = true });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetTracksAsync(category: "sleep", search: null, isPremium: null, sortBy: "newest", page: 1, pageSize: 10, currentUserId: null);

            // Assert
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().ContainSingle();
            response.Data.Items.First().Title.Should().Be("Sleep Sound");
        }

        // ==========================================
        // --- 3. GetTracks_SearchByTitleOrArtist ---
        // ==========================================
        [Fact]
        public async Task GetTracks_SearchByTitleOrArtist()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var cat = new AudioCategory { Id = Guid.NewGuid(), Name = "focus", IsActive = true };
            context.AudioCategories.Add(cat);

            context.AudioTracks.Add(new AudioTrack { Id = Guid.NewGuid(), Title = "Deep Focus Binaural", Artist = "Zen Master", CategoryId = cat.Id, FileUrl = "focus_key1", IsActive = true });
            context.AudioTracks.Add(new AudioTrack { Id = Guid.NewGuid(), Title = "Coding Vibe", Artist = "DJ Antigravity", CategoryId = cat.Id, FileUrl = "focus_key2", IsActive = true });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act (Search Title)
            var response1 = await service.GetTracksAsync(category: null, search: "binaural", isPremium: null, sortBy: "newest", page: 1, pageSize: 10, currentUserId: null);
            // Act (Search Artist)
            var response2 = await service.GetTracksAsync(category: null, search: "Antigravity", isPremium: null, sortBy: "newest", page: 1, pageSize: 10, currentUserId: null);

            // Assert
            response1.Data!.Items.Should().ContainSingle();
            response1.Data.Items.First().Title.Should().Be("Deep Focus Binaural");

            response2.Data!.Items.Should().ContainSingle();
            response2.Data.Items.First().Artist.Should().Be("DJ Antigravity");
        }

        // ==========================================
        // --- 4. GetTracks_DoesNotExposeFileUrl ---
        // ==========================================
        [Fact]
        public async Task GetTracks_DoesNotExposeFileUrl()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetTracksAsync(category: null, search: null, isPremium: null, sortBy: "newest", page: 1, pageSize: 10, currentUserId: null);

            // Assert
            response.Success.Should().BeTrue();
            // AudioTrackDto does not even have a FileUrl property. We verify that FileUrl/fileKey is not exposed in the Dto.
            // Let's also check detail Dto.
            var trackDto = response.Data!.Items.First();
            // Direct inspection of serialized JSON would show no "FileUrl" or "fileKey" is present.
            trackDto.Title.Should().Be("Morning Calm");
        }

        // ==========================================
        // --- 5. GetTrackById_Success -------------
        // ==========================================
        [Fact]
        public async Task GetTrackById_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetTrackByIdAsync(trackId, currentUserId: null);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(trackId);
            response.Data.Title.Should().Be("Morning Calm");
        }

        // ==========================================
        // --- 6. GetTrackById_NotFound ------------
        // ==========================================
        [Fact]
        public async Task GetTrackById_NotFound()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetTrackByIdAsync(Guid.NewGuid(), currentUserId: null);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.AUDIO_TRACK_NOT_FOUND.ToString());
        }

        // ==========================================
        // --- 7. GetStreamUrl_ReturnsPresignedUrl -
        // ==========================================
        [Fact]
        public async Task GetStreamUrl_ReturnsPresignedUrl()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context, isPremium: false);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetStreamUrlAsync(trackId, userId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.StreamUrl.Should().Contain("calm.mp3");
            response.Data.StreamUrl.Should().Contain("expires=60");

            _fileStorageMock.Verify(f => f.GeneratePresignedDownloadUrlAsync("audio/tracks/calm.mp3", 60), Times.Once);
        }

        // ==========================================
        // --- 8. CreateTrack_AdminOnly_Success -----
        // ==========================================
        [Fact]
        public async Task CreateTrack_AdminOnly_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var cat = new AudioCategory { Id = Guid.NewGuid(), Name = "relaxation", IsActive = true };
            context.AudioCategories.Add(cat);
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            var dto = new CreateAudioTrackDto
            {
                Title = "Deep Sleep Rain",
                Artist = "Nature Sounds",
                CategoryId = cat.Id,
                DurationSeconds = 1800,
                FileUrl = "audio/tracks/rain.mp3",
                CoverUrl = "https://public-r2.com/covers/rain.webp",
                IsPremium = true
            };

            // Act
            var response = await service.CreateTrackAsync(dto, adminId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Title.Should().Be("Deep Sleep Rain");
            response.Data.IsPremium.Should().BeTrue();

            context.AudioTracks.Count(t => t.DeletedAt == null).Should().Be(1);
            var dbTrack = context.AudioTracks.First();
            dbTrack.Title.Should().Be("Deep Sleep Rain");
            dbTrack.FileUrl.Should().Be("audio/tracks/rain.mp3");
            dbTrack.UploadedBy.Should().Be(adminId);
        }

        // ==========================================
        // --- 9. CreateTrack_NonAdmin_ReturnsForbidden ---
        // ==========================================
        [Fact]
        public async Task CreateTrack_NonAdmin_ReturnsForbidden()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid(); // Not an admin
            var cat = new AudioCategory { Id = Guid.NewGuid(), Name = "relaxation", IsActive = true };
            context.AudioCategories.Add(cat);
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);
            var dto = new CreateAudioTrackDto { Title = "Test Track", CategoryId = cat.Id, FileUrl = "test.mp3" };

            // Act
            var response = await service.CreateTrackAsync(dto, userId);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.FORBIDDEN.ToString());
            context.AudioTracks.Should().BeEmpty();
        }

        // ==========================================
        // --- 10. CreateTrack_InvalidCategory_Fails ---
        // ==========================================
        [Fact]
        public async Task CreateTrack_InvalidCategory_Fails()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);
            var dto = new CreateAudioTrackDto { Title = "Test Track", CategoryId = Guid.NewGuid(), FileUrl = "test.mp3" }; // Invalid category ID

            // Act
            var response = await service.CreateTrackAsync(dto, adminId);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.AUDIO_CATEGORY_INVALID.ToString());
        }

        // ==========================================
        // --- 11. UpdateTrack_AdminOnly ------------
        // ==========================================
        [Fact]
        public async Task UpdateTrack_AdminOnly()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var (catId, trackId) = await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);
            var dto = new UpdateAudioTrackDto
            {
                Title = "Morning Calm - Remastered",
                Artist = "Zen Master Extra",
                IsPremium = true
            };

            // Act
            var response = await service.UpdateTrackAsync(trackId, dto, adminId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data!.Title.Should().Be("Morning Calm - Remastered");
            response.Data.Artist.Should().Be("Zen Master Extra");
            response.Data.IsPremium.Should().BeTrue();

            var dbTrack = context.AudioTracks.Find(trackId);
            dbTrack!.Title.Should().Be("Morning Calm - Remastered");
        }

        // ==========================================
        // --- 12. DeleteTrack_SoftDelete -----------
        // ==========================================
        [Fact]
        public async Task DeleteTrack_SoftDelete()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.DeleteTrackAsync(trackId, adminId);

            // Assert
            response.Success.Should().BeTrue();

            var dbTrack = context.AudioTracks.Find(trackId);
            dbTrack.Should().NotBeNull();
            dbTrack!.DeletedAt.Should().NotBeNull();
            dbTrack.IsActive.Should().BeFalse();
        }

        // ==========================================
        // --- 13. RecordPlay_IncrementsPlayCount ---
        // ==========================================
        [Fact]
        public async Task RecordPlay_IncrementsPlayCount()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);
            var dto = new RecordPlayDto { TrackId = trackId, PlayedSeconds = 120 };

            // Act
            var response = await service.RecordPlayAsync(dto, userId);

            // Assert
            response.Success.Should().BeTrue();

            var dbTrack = context.AudioTracks.Find(trackId);
            dbTrack!.PlayCount.Should().Be(1);

            context.UserAudioHistories.Should().ContainSingle();
            var dbHistory = context.UserAudioHistories.First();
            dbHistory.UserId.Should().Be(userId);
            dbHistory.TrackId.Should().Be(trackId);
            dbHistory.PlayedSeconds.Should().Be(120);
        }

        // ==========================================
        // --- 14. GetPlayHistory_ReturnsPaged ------
        // ==========================================
        [Fact]
        public async Task GetPlayHistory_ReturnsPaged()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            context.UserAudioHistories.Add(new UserAudioHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrackId = trackId,
                PlayedSeconds = 100,
                PlayedAt = DateTime.UtcNow.AddMinutes(-10)
            });
            context.UserAudioHistories.Add(new UserAudioHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrackId = trackId,
                PlayedSeconds = 200,
                PlayedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetPlayHistoryAsync(userId, page: 1, pageSize: 10);

            // Assert
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(2);
            response.Data.Items.First().PlayedSeconds.Should().Be(200); // Sorted descending by PlayedAt
            response.Data.Items.Last().PlayedSeconds.Should().Be(100);
        }

        // ==========================================
        // --- 15. GetListeningStats_Aggregation ----
        // ==========================================
        [Fact]
        public async Task GetListeningStats_Aggregation()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context, "meditation");

            context.UserAudioHistories.Add(new UserAudioHistory { Id = Guid.NewGuid(), UserId = userId, TrackId = trackId, PlayedSeconds = 300, PlayedAt = DateTime.UtcNow });
            context.UserAudioHistories.Add(new UserAudioHistory { Id = Guid.NewGuid(), UserId = userId, TrackId = trackId, PlayedSeconds = 400, PlayedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetListeningStatsAsync(userId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data!.TotalTracksPlayed.Should().Be(1);
            response.Data.TotalSecondsListened.Should().Be(700);
            response.Data.MostPlayedCategory.Should().Be("meditation");
        }

        // ==========================================
        // --- 16. AddFavorite_Success --------------
        // ==========================================
        [Fact]
        public async Task AddFavorite_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.AddFavoriteAsync(trackId, userId);

            // Assert
            response.Success.Should().BeTrue();
            context.UserFavoriteTracks.Should().ContainSingle();
            var fav = context.UserFavoriteTracks.First();
            fav.UserId.Should().Be(userId);
            fav.TrackId.Should().Be(trackId);
        }

        // ==========================================
        // --- 17. AddFavorite_AlreadyFavorited_Fails ---
        // ==========================================
        [Fact]
        public async Task AddFavorite_AlreadyFavorited_Fails()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            context.UserFavoriteTracks.Add(new UserFavoriteTrack { Id = Guid.NewGuid(), UserId = userId, TrackId = trackId });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.AddFavoriteAsync(trackId, userId);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.AUDIO_ALREADY_FAVORITED.ToString());
        }

        // ==========================================
        // --- 18. RemoveFavorite_Success -----------
        // ==========================================
        [Fact]
        public async Task RemoveFavorite_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var userId = Guid.NewGuid();
            var (_, trackId) = await SetupTrackAndCategoryAsync(context);

            var fav = new UserFavoriteTrack { Id = Guid.NewGuid(), UserId = userId, TrackId = trackId };
            context.UserFavoriteTracks.Add(fav);
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.RemoveFavoriteAsync(trackId, userId);

            // Assert
            response.Success.Should().BeTrue();
            context.UserFavoriteTracks.Should().BeEmpty();
        }

        // ==========================================
        // --- 19. GetCategories_ReturnsSorted ------
        // ==========================================
        [Fact]
        public async Task GetCategories_ReturnsSorted()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "sleep", SortOrder = 10, IsActive = true });
            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "meditation", SortOrder = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetCategoriesAsync();

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().HaveCount(2);
            response.Data!.First().Name.Should().Be("meditation"); // SortOrder 1 comes before 10
            response.Data!.Last().Name.Should().Be("sleep");
        }

        // ==========================================
        // --- 20. CreateCategory_AdminOnly ---------
        // ==========================================
        [Fact]
        public async Task CreateCategory_AdminOnly()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);
            var dto = new CreateAudioCategoryDto
            {
                Name = "nature",
                Description = "Nature and forest soundscapes",
                SortOrder = 5
            };

            // Act
            var response = await service.CreateCategoryAsync(dto, adminId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Name.Should().Be("nature");

            context.AudioCategories.Should().ContainSingle();
            context.AudioCategories.First().Name.Should().Be("nature");
        }

        // ==========================================
        // --- 21. DeleteCategory_InUse_Fails -------
        // ==========================================
        [Fact]
        public async Task DeleteCategory_InUse_Fails()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            var (catId, _) = await SetupTrackAndCategoryAsync(context, "relaxation");

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.DeleteCategoryAsync(catId, adminId);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.AUDIO_CATEGORY_IN_USE.ToString());

            context.AudioCategories.Should().ContainSingle(); // Category was not deleted
        }

        // ==========================================
        // --- 22. GetAllCategoriesForAdmin_Success -
        // ==========================================
        [Fact]
        public async Task GetAllCategoriesForAdmin_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var adminId = Guid.NewGuid();
            await SetupAdminRoleAsync(context, adminId);

            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "active-cat", IsActive = true, SortOrder = 1 });
            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "inactive-cat", IsActive = false, SortOrder = 2 });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetAllCategoriesForAdminAsync(adminId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().HaveCount(2);
            response.Data!.Any(c => c.Name == "inactive-cat" && !c.IsActive).Should().BeTrue();
        }

        // ==========================================
        // --- 23. GetAllCategories_RegularUser_Success
        // ==========================================
        [Fact]
        public async Task GetAllCategories_RegularUser_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var regularUserId = Guid.NewGuid(); // Not an admin

            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "active-cat", IsActive = true, SortOrder = 1 });
            context.AudioCategories.Add(new AudioCategory { Id = Guid.NewGuid(), Name = "inactive-cat", IsActive = false, SortOrder = 2 });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetAllCategoriesForAdminAsync(regularUserId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().HaveCount(2);
        }

        // ==========================================
        // --- 24. GetCategoryById_Success ----------
        // ==========================================
        [Fact]
        public async Task GetCategoryById_Success()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var catId = Guid.NewGuid();
            context.AudioCategories.Add(new AudioCategory 
            { 
                Id = catId, 
                Name = "breathing", 
                Description = "Breathing exercises",
                IsActive = true,
                SortOrder = 3
            });
            await context.SaveChangesAsync();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetCategoryByIdAsync(catId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(catId);
            response.Data.Name.Should().Be("breathing");
            response.Data.Description.Should().Be("Breathing exercises");
        }

        // ==========================================
        // --- 25. GetCategoryById_NotFound ---------
        // ==========================================
        [Fact]
        public async Task GetCategoryById_NotFound()
        {
            // Arrange
            using var context = DbContextFactory.Create();
            var nonExistentId = Guid.NewGuid();

            var service = new AudioTrackService(context, _fileStorageMock.Object, _loggerMock.Object);

            // Act
            var response = await service.GetCategoryByIdAsync(nonExistentId);

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be(ErrorCode.AUDIO_CATEGORY_NOT_FOUND.ToString());
        }
    }
}
