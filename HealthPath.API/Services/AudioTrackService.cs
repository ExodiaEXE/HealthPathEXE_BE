using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public class AudioTrackService : IAudioTrackService
{
    private readonly HealthpathDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AudioTrackService> _logger;

    public AudioTrackService(
        HealthpathDbContext dbContext,
        IFileStorageService fileStorageService,
        ILogger<AudioTrackService> logger)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    // --- Helper Methods ---

    private async Task<bool> IsAdminAsync(Guid userId)
    {
        if (await _dbContext.Admins.AnyAsync(a => a.Id == userId && a.IsActive))
        {
            return true;
        }

        return await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId
                         && ur.Role.Name.ToLower() == "admin"
                         && ur.DeletedAt == null);
    }

    private async Task<bool> HasPremiumAccessAsync(Guid userId)
    {
        if (await IsAdminAsync(userId)) return true;

        return await _dbContext.UserSubscriptions
            .AnyAsync(s => s.UserId == userId 
                         && s.Status.ToLower() == "active" 
                         && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow) 
                         && s.DeletedAt == null);
    }

    // --- Browse & Search (All users) ---

    public async Task<ApiResponse<PageResponse<AudioTrackDto>>> GetTracksAsync(
        string? category, string? search, bool? isPremium,
        string sortBy, int page, int pageSize, Guid? currentUserId)
    {
        var query = _dbContext.AudioTracks
            .Include(t => t.Category)
            .Where(t => t.IsActive && t.DeletedAt == null);

        // Filter by category name
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category.Name.ToLower() == category.ToLower());
        }

        // Filter by search keyword (Title or Artist)
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchLower) 
                                  || (t.Artist != null && t.Artist.ToLower().Contains(searchLower)));
        }

        // Filter by Premium state
        if (isPremium.HasValue)
        {
            query = query.Where(t => t.IsPremium == isPremium.Value);
        }

        // Sorting
        query = sortBy.ToLower() switch
        {
            "popular" => query.OrderByDescending(t => t.PlayCount),
            "title" => query.OrderBy(t => t.Title),
            _ => query.OrderByDescending(t => t.CreatedAt) // "newest" or default
        };

        var totalItems = await query.CountAsync();

        var tracks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Check favorites if currentUserId is provided
        var favoriteTrackIds = new HashSet<Guid>();
        if (currentUserId.HasValue)
        {
            var favs = await _dbContext.UserFavoriteTracks
                .Where(f => f.UserId == currentUserId.Value)
                .Select(f => f.TrackId)
                .ToListAsync();
            favoriteTrackIds = new HashSet<Guid>(favs);
        }

        var dtos = tracks.Select(t => new AudioTrackDto
        {
            Id = t.Id,
            Title = t.Title,
            Artist = t.Artist,
            Studio = t.Studio,
            Category = t.Category.Name,
            CategoryId = t.CategoryId,
            DurationSeconds = t.DurationSeconds,
            CoverUrl = t.CoverUrl,
            IsPremium = t.IsPremium,
            PlayCount = t.PlayCount,
            IsFavorited = favoriteTrackIds.Contains(t.Id),
            CreatedAt = t.CreatedAt
        }).ToList();

        var result = new PageResponse<AudioTrackDto>(dtos, totalItems, page, pageSize);
        return ApiResponse<PageResponse<AudioTrackDto>>.Ok(result);
    }

    public async Task<ApiResponse<AudioTrackDetailDto>> GetTrackByIdAsync(Guid trackId, Guid? currentUserId)
    {
        var track = await _dbContext.AudioTracks
            .Include(t => t.Category)
            .Include(t => t.UploadedByNavigation)
            .FirstOrDefaultAsync(t => t.Id == trackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<AudioTrackDetailDto>.Fail("Không tìm thấy bài hát", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        bool isFavorited = false;
        if (currentUserId.HasValue)
        {
            isFavorited = await _dbContext.UserFavoriteTracks
                .AnyAsync(f => f.UserId == currentUserId.Value && f.TrackId == trackId);
        }

        var dto = new AudioTrackDetailDto
        {
            Id = track.Id,
            Title = track.Title,
            Artist = track.Artist,
            Studio = track.Studio,
            Category = track.Category.Name,
            CategoryId = track.CategoryId,
            DurationSeconds = track.DurationSeconds,
            CoverUrl = track.CoverUrl,
            IsPremium = track.IsPremium,
            PlayCount = track.PlayCount,
            IsFavorited = isFavorited,
            CreatedAt = track.CreatedAt,
            UploadedBy = track.UploadedBy,
            UploadedByName = track.UploadedByNavigation?.FullName,
            UpdatedAt = track.UpdatedAt
        };

        return ApiResponse<AudioTrackDetailDto>.Ok(dto);
    }

    // --- Streaming (Presigned URL) ---

    public async Task<ApiResponse<AudioStreamUrlDto>> GetStreamUrlAsync(Guid trackId, Guid userId)
    {
        var track = await _dbContext.AudioTracks
            .FirstOrDefaultAsync(t => t.Id == trackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<AudioStreamUrlDto>.Fail("Không tìm thấy bài hát", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        if (!track.IsActive)
        {
            return ApiResponse<AudioStreamUrlDto>.Fail("Bài hát hiện tại đang bị vô hiệu hóa", ErrorCode.FORBIDDEN);
        }

        // Check premium access
        if (track.IsPremium && !(await HasPremiumAccessAsync(userId)))
        {
            return ApiResponse<AudioStreamUrlDto>.Fail("Bài hát này yêu cầu tài khoản Premium", ErrorCode.PREMIUM_REQUIRED);
        }

        // URL phát nhạc: ưu tiên public CDN (R2 pub domain), fallback presigned
        var streamUrl = await _fileStorageService.GetPlaybackUrlAsync(track.FileUrl, 60);

        var result = new AudioStreamUrlDto
        {
            StreamUrl = streamUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return ApiResponse<AudioStreamUrlDto>.Ok(result, "Lấy link stream thành công");
    }

    // --- CRUD (Admin only) ---

    public async Task<ApiResponse<AudioTrackDto>> CreateTrackAsync(CreateAudioTrackDto dto, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<AudioTrackDto>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        // Validate category
        var category = await _dbContext.AudioCategories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.IsActive);
        
        if (category == null)
        {
            return ApiResponse<AudioTrackDto>.Fail("Danh mục không tồn tại hoặc đã bị tắt", ErrorCode.AUDIO_CATEGORY_INVALID);
        }

        var uploadedBy = await _dbContext.Users.AnyAsync(u => u.Id == adminUserId)
            ? adminUserId
            : (Guid?)null;

        var track = new AudioTrack
        {
            Title = dto.Title,
            Artist = dto.Artist,
            Studio = dto.Studio,
            CategoryId = dto.CategoryId,
            DurationSeconds = dto.DurationSeconds,
            FileUrl = dto.FileUrl, // fileKey nhận được từ upload endpoint
            CoverUrl = dto.CoverUrl,
            IsPremium = dto.IsPremium,
            IsActive = true,
            PlayCount = 0,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.AudioTracks.Add(track);
        await _dbContext.SaveChangesAsync();

        var result = new AudioTrackDto
        {
            Id = track.Id,
            Title = track.Title,
            Artist = track.Artist,
            Studio = track.Studio,
            Category = category.Name,
            CategoryId = track.CategoryId,
            DurationSeconds = track.DurationSeconds,
            CoverUrl = track.CoverUrl,
            IsPremium = track.IsPremium,
            PlayCount = track.PlayCount,
            IsFavorited = false,
            CreatedAt = track.CreatedAt
        };

        return ApiResponse<AudioTrackDto>.Ok(result, "Tạo bài hát mới thành công");
    }

    public async Task<ApiResponse<AudioTrackDto>> UpdateTrackAsync(Guid trackId, UpdateAudioTrackDto dto, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<AudioTrackDto>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        var track = await _dbContext.AudioTracks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == trackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<AudioTrackDto>.Fail("Không tìm thấy bài hát cần cập nhật", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        // Update category if provided
        AudioCategory? newCategory = null;
        if (dto.CategoryId.HasValue && dto.CategoryId.Value != track.CategoryId)
        {
            newCategory = await _dbContext.AudioCategories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && c.IsActive);
            
            if (newCategory == null)
            {
                return ApiResponse<AudioTrackDto>.Fail("Danh mục mới không hợp lệ", ErrorCode.AUDIO_CATEGORY_INVALID);
            }
            track.CategoryId = dto.CategoryId.Value;
        }

        // Update optional fields
        if (dto.Title != null) track.Title = dto.Title;
        if (dto.Artist != null) track.Artist = dto.Artist;
        if (dto.Studio != null) track.Studio = dto.Studio;
        if (dto.DurationSeconds.HasValue) track.DurationSeconds = dto.DurationSeconds.Value;
        if (dto.CoverUrl != null) track.CoverUrl = dto.CoverUrl;
        if (dto.IsPremium.HasValue) track.IsPremium = dto.IsPremium.Value;
        if (dto.IsActive.HasValue) track.IsActive = dto.IsActive.Value;

        track.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var result = new AudioTrackDto
        {
            Id = track.Id,
            Title = track.Title,
            Artist = track.Artist,
            Studio = track.Studio,
            Category = newCategory?.Name ?? track.Category.Name,
            CategoryId = track.CategoryId,
            DurationSeconds = track.DurationSeconds,
            CoverUrl = track.CoverUrl,
            IsPremium = track.IsPremium,
            PlayCount = track.PlayCount,
            CreatedAt = track.CreatedAt
        };

        return ApiResponse<AudioTrackDto>.Ok(result, "Cập nhật thông tin bài hát thành công");
    }

    public async Task<ApiResponse<object>> DeleteTrackAsync(Guid trackId, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<object>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        var track = await _dbContext.AudioTracks
            .FirstOrDefaultAsync(t => t.Id == trackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<object>.Fail("Không tìm thấy bài hát cần xóa", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        // Soft delete
        track.DeletedAt = DateTime.UtcNow;
        track.IsActive = false;
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Xóa bài hát thành công");
    }

    // --- Categories Management (Admin CRUD + User Read) ---

    public async Task<ApiResponse<List<AudioCategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _dbContext.AudioCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new AudioCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconUrl = c.IconUrl,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder
            }).ToListAsync();

        return ApiResponse<List<AudioCategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<List<AudioCategoryDto>>> GetAllCategoriesForAdminAsync(Guid adminUserId)
    {
        var categories = await _dbContext.AudioCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new AudioCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconUrl = c.IconUrl,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder
            }).ToListAsync();

        return ApiResponse<List<AudioCategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<AudioCategoryDto>> GetCategoryByIdAsync(Guid categoryId)
    {
        var category = await _dbContext.AudioCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
        {
            return ApiResponse<AudioCategoryDto>.Fail("Không tìm thấy danh mục", ErrorCode.AUDIO_CATEGORY_NOT_FOUND);
        }

        var dto = new AudioCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IconUrl = category.IconUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder
        };

        return ApiResponse<AudioCategoryDto>.Ok(dto);
    }

    public async Task<ApiResponse<AudioCategoryDto>> CreateCategoryAsync(CreateAudioCategoryDto dto, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<AudioCategoryDto>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        // Check unique name
        var nameLower = dto.Name.ToLower();
        if (await _dbContext.AudioCategories.AnyAsync(c => c.Name.ToLower() == nameLower))
        {
            return ApiResponse<AudioCategoryDto>.Fail("Tên danh mục này đã tồn tại", ErrorCode.AUDIO_CATEGORY_NAME_TAKEN);
        }

        var category = new AudioCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            IconUrl = dto.IconUrl,
            IsActive = true,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AudioCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        var result = new AudioCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IconUrl = category.IconUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder
        };

        return ApiResponse<AudioCategoryDto>.Ok(result, "Tạo danh mục mới thành công");
    }

    public async Task<ApiResponse<AudioCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateAudioCategoryDto dto, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<AudioCategoryDto>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        var category = await _dbContext.AudioCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
        {
            return ApiResponse<AudioCategoryDto>.Fail("Không tìm thấy danh mục cần cập nhật", ErrorCode.AUDIO_CATEGORY_NOT_FOUND);
        }

        // Check unique name if changed
        if (dto.Name != null && dto.Name.ToLower() != category.Name.ToLower())
        {
            var nameLower = dto.Name.ToLower();
            if (await _dbContext.AudioCategories.AnyAsync(c => c.Name.ToLower() == nameLower && c.Id != categoryId))
            {
                return ApiResponse<AudioCategoryDto>.Fail("Tên danh mục mới đã được sử dụng", ErrorCode.AUDIO_CATEGORY_NAME_TAKEN);
            }
            category.Name = dto.Name;
        }

        if (dto.Description != null) category.Description = dto.Description;
        if (dto.IconUrl != null) category.IconUrl = dto.IconUrl;
        if (dto.SortOrder.HasValue) category.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

        await _dbContext.SaveChangesAsync();

        var result = new AudioCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IconUrl = category.IconUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder
        };

        return ApiResponse<AudioCategoryDto>.Ok(result, "Cập nhật danh mục thành công");
    }

    public async Task<ApiResponse<object>> DeleteCategoryAsync(Guid categoryId, Guid adminUserId)
    {
        if (!(await IsAdminAsync(adminUserId)))
        {
            return ApiResponse<object>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN);
        }

        var category = await _dbContext.AudioCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
        {
            return ApiResponse<object>.Fail("Không tìm thấy danh mục cần xóa", ErrorCode.AUDIO_CATEGORY_NOT_FOUND);
        }

        // Check if any active tracks are using this category
        var isCategoryInUse = await _dbContext.AudioTracks
            .AnyAsync(t => t.CategoryId == categoryId && t.DeletedAt == null);

        if (isCategoryInUse)
        {
            return ApiResponse<object>.Fail("Không thể xóa danh mục này vì đang có bài hát thuộc danh mục", ErrorCode.AUDIO_CATEGORY_IN_USE);
        }

        // Hard delete or deactivate
        // Vì bảng category đơn giản, ta sẽ xóa cứng khỏi DB nếu không còn nhạc dùng
        _dbContext.AudioCategories.Remove(category);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Xóa danh mục thành công");
    }

    // --- Play History ---

    public async Task<ApiResponse<object>> RecordPlayAsync(RecordPlayDto dto, Guid userId)
    {
        var track = await _dbContext.AudioTracks
            .FirstOrDefaultAsync(t => t.Id == dto.TrackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<object>.Fail("Không tìm thấy bài hát", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        // 1. Tạo bản ghi lịch sử nghe
        var history = new UserAudioHistory
        {
            UserId = userId,
            TrackId = dto.TrackId,
            PlayedSeconds = dto.PlayedSeconds,
            PlayedAt = DateTime.UtcNow
        };

        _dbContext.UserAudioHistories.Add(history);

        // 2. Tăng số lần phát nhạc của bài hát (atomic)
        track.PlayCount += 1;
        track.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Ghi nhận lịch sử nghe nhạc thành công");
    }

    public async Task<ApiResponse<PageResponse<AudioHistoryDto>>> GetPlayHistoryAsync(Guid userId, int page, int pageSize)
    {
        var query = _dbContext.UserAudioHistories
            .Include(h => h.Track)
            .ThenInclude(t => t.Category)
            .Where(h => h.UserId == userId && h.DeletedAt == null && h.Track.DeletedAt == null);

        var totalItems = await query.CountAsync();

        var histories = await query
            .OrderByDescending(h => h.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = histories.Select(h => new AudioHistoryDto
        {
            Id = h.Id,
            TrackId = h.TrackId,
            TrackTitle = h.Track.Title,
            TrackCoverUrl = h.Track.CoverUrl,
            TrackArtist = h.Track.Artist,
            TrackCategory = h.Track.Category.Name,
            PlayedSeconds = h.PlayedSeconds,
            PlayedAt = h.PlayedAt
        }).ToList();

        var result = new PageResponse<AudioHistoryDto>(dtos, totalItems, page, pageSize);
        return ApiResponse<PageResponse<AudioHistoryDto>>.Ok(result);
    }

    public async Task<ApiResponse<AudioStatsDto>> GetListeningStatsAsync(Guid userId)
    {
        var histories = await _dbContext.UserAudioHistories
            .Include(h => h.Track)
            .ThenInclude(t => t.Category)
            .Where(h => h.UserId == userId && h.DeletedAt == null && h.Track.DeletedAt == null)
            .ToListAsync();

        if (!histories.Any())
        {
            return ApiResponse<AudioStatsDto>.Ok(new AudioStatsDto
            {
                TotalTracksPlayed = 0,
                TotalSecondsListened = 0,
                MostPlayedCategory = "Chưa có"
            });
        }

        var totalTracks = histories.Select(h => h.TrackId).Distinct().Count();
        var totalSeconds = histories.Sum(h => (long)h.PlayedSeconds);

        // Group by category to find the most played one
        var mostPlayedCategory = histories
            .GroupBy(h => h.Track.Category.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var result = new AudioStatsDto
        {
            TotalTracksPlayed = totalTracks,
            TotalSecondsListened = totalSeconds,
            MostPlayedCategory = mostPlayedCategory ?? "Chưa có"
        };

        return ApiResponse<AudioStatsDto>.Ok(result);
    }

    // --- Favorites Management ---

    public async Task<ApiResponse<object>> AddFavoriteAsync(Guid trackId, Guid userId)
    {
        var track = await _dbContext.AudioTracks
            .FirstOrDefaultAsync(t => t.Id == trackId && t.DeletedAt == null);

        if (track == null)
        {
            return ApiResponse<object>.Fail("Không tìm thấy bài hát", ErrorCode.AUDIO_TRACK_NOT_FOUND);
        }

        // Check if already favorited
        var isFavorited = await _dbContext.UserFavoriteTracks
            .AnyAsync(f => f.UserId == userId && f.TrackId == trackId);

        if (isFavorited)
        {
            return ApiResponse<object>.Fail("Bài hát này đã có trong danh sách yêu thích", ErrorCode.AUDIO_ALREADY_FAVORITED);
        }

        var favorite = new UserFavoriteTrack
        {
            UserId = userId,
            TrackId = trackId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserFavoriteTracks.Add(favorite);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Đã thêm vào danh sách yêu thích");
    }

    public async Task<ApiResponse<object>> RemoveFavoriteAsync(Guid trackId, Guid userId)
    {
        var favorite = await _dbContext.UserFavoriteTracks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TrackId == trackId);

        if (favorite == null)
        {
            return ApiResponse<object>.Fail("Bài hát này chưa có trong danh sách yêu thích", ErrorCode.AUDIO_NOT_FAVORITED);
        }

        _dbContext.UserFavoriteTracks.Remove(favorite);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Đã xóa khỏi danh sách yêu thích");
    }

    public async Task<ApiResponse<PageResponse<AudioTrackDto>>> GetFavoritesAsync(Guid userId, int page, int pageSize)
    {
        var query = _dbContext.UserFavoriteTracks
            .Include(f => f.Track)
            .ThenInclude(t => t.Category)
            .Where(f => f.UserId == userId && f.Track.IsActive && f.Track.DeletedAt == null);

        var totalItems = await query.CountAsync();

        var favorites = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = favorites.Select(f => new AudioTrackDto
        {
            Id = f.TrackId,
            Title = f.Track.Title,
            Artist = f.Track.Artist,
            Studio = f.Track.Studio,
            Category = f.Track.Category.Name,
            CategoryId = f.Track.CategoryId,
            DurationSeconds = f.Track.DurationSeconds,
            CoverUrl = f.Track.CoverUrl,
            IsPremium = f.Track.IsPremium,
            PlayCount = f.Track.PlayCount,
            IsFavorited = true, // hiển nhiên trong trang favorites
            CreatedAt = f.Track.CreatedAt
        }).ToList();

        var result = new PageResponse<AudioTrackDto>(dtos, totalItems, page, pageSize);
        return ApiResponse<PageResponse<AudioTrackDto>>.Ok(result);
    }
}
