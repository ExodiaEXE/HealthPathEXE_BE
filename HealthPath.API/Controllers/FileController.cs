using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;

namespace HealthPath.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly HealthpathDbContext _dbContext;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav", ".ogg", ".flac" };
    
    private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB
    private const long MaxAudioSize = 50 * 1024 * 1024; // 50 MB

    public FileController(IFileStorageService fileStorageService, HealthpathDbContext dbContext)
    {
        _fileStorageService = fileStorageService;
        _dbContext = dbContext;
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var validationResult = ValidateFile(file, AllowedImageExtensions, MaxImageSize);
        if (validationResult != null) return BadRequest(validationResult);

        var userId = User.GetUserId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found", ErrorCode.INTERNAL_ERROR));
        }

        // Delete old avatar from storage if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            await _fileStorageService.DeleteAsync(user.AvatarUrl);
        }

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, $"avatars/{userId}");

        user.AvatarUrl = url;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var result = new FileUploadResultDto
        {
            Url = url,
            FileKey = ExtractKey(url),
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result));
    }

    [HttpPost("routine/{routineId}/thumbnail")]
    public async Task<IActionResult> UploadRoutineThumbnail(Guid routineId, IFormFile file)
    {
        var validationResult = ValidateFile(file, AllowedImageExtensions, MaxImageSize);
        if (validationResult != null) return BadRequest(validationResult);

        var routine = await _dbContext.Routines.FirstOrDefaultAsync(r => r.Id == routineId && r.DeletedAt == null);
        if (routine == null)
        {
            return NotFound(ApiResponse<object>.Fail("Routine not found", ErrorCode.ROUTINE_NOT_FOUND));
        }

        // Delete old thumbnail if exists
        if (!string.IsNullOrEmpty(routine.ThumbnailUrl))
        {
            await _fileStorageService.DeleteAsync(routine.ThumbnailUrl);
        }

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, $"routines/thumbnails/{routineId}");

        routine.ThumbnailUrl = url;
        routine.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var result = new FileUploadResultDto
        {
            Url = url,
            FileKey = ExtractKey(url),
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result));
    }

    [HttpPost("group/{groupId}/cover")]
    public async Task<IActionResult> UploadGroupCover(Guid groupId, IFormFile file)
    {
        var validationResult = ValidateFile(file, AllowedImageExtensions, MaxImageSize);
        if (validationResult != null) return BadRequest(validationResult);

        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId && g.DeletedAt == null);
        if (group == null)
        {
            return NotFound(ApiResponse<object>.Fail("Group not found", ErrorCode.INTERNAL_ERROR));
        }

        // Delete old cover if exists
        if (!string.IsNullOrEmpty(group.CoverUrl))
        {
            await _fileStorageService.DeleteAsync(group.CoverUrl);
        }

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, $"groups/covers/{groupId}");

        group.CoverUrl = url;
        group.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var result = new FileUploadResultDto
        {
            Url = url,
            FileKey = ExtractKey(url),
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result));
    }

    /// <summary>
    /// Liệt kê các file MP3 đã upload (Admin). Đánh dấu file nào đã đăng ký vào AudioTrack.
    /// </summary>
    [HttpGet("audio/tracks")]
    public async Task<IActionResult> ListUploadedAudioTracks()
    {
        if (!await CanPerformAdminFileOpsAsync())
        {
            return StatusCode(403, ApiResponse<object>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN));
        }

        var files = await _fileStorageService.ListFilesAsync("audio/tracks");
        var registeredTracks = await _dbContext.AudioTracks
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.Id, t.Title, t.FileUrl })
            .ToListAsync();

        var trackByKey = registeredTracks
            .GroupBy(t => NormalizeFileKey(t.FileUrl))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = files.Select(file =>
        {
            var normalizedKey = NormalizeFileKey(file.FileKey);
            trackByKey.TryGetValue(normalizedKey, out var track);

            return new StoredAudioFileDto
            {
                FileKey = file.FileKey,
                Url = file.Url,
                SizeBytes = file.SizeBytes,
                UploadedAt = file.LastModified,
                IsRegistered = track != null,
                TrackId = track?.Id,
                TrackTitle = track?.Title
            };
        }).ToList();

        return Ok(ApiResponse<List<StoredAudioFileDto>>.Ok(result));
    }

    /// <summary>
    /// Liệt kê các ảnh bìa audio đã upload (Admin).
    /// </summary>
    [HttpGet("audio/covers")]
    public async Task<IActionResult> ListUploadedAudioCovers()
    {
        if (!await CanPerformAdminFileOpsAsync())
        {
            return StatusCode(403, ApiResponse<object>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN));
        }

        var files = await _fileStorageService.ListFilesAsync("audio/covers");
        var result = files.Select(file => new StoredAudioFileDto
        {
            FileKey = file.FileKey,
            Url = file.Url,
            SizeBytes = file.SizeBytes,
            UploadedAt = file.LastModified,
            IsRegistered = false
        }).ToList();

        return Ok(ApiResponse<List<StoredAudioFileDto>>.Ok(result));
    }

    /// <summary>
    /// Hướng dẫn đăng ký bài hát qua POST /api/AudioTrack sau khi upload file (Admin).
    /// Truyền fileKey/coverUrl để nhận body mẫu đã điền sẵn.
    /// </summary>
    [HttpGet("audio/registration-info")]
    public async Task<IActionResult> GetAudioTrackRegistrationInfo(
        [FromQuery] string? fileKey,
        [FromQuery] string? coverUrl)
    {
        if (!await CanPerformAdminFileOpsAsync())
        {
            return StatusCode(403, ApiResponse<object>.Fail("Chỉ Admin mới có quyền thực hiện thao tác này", ErrorCode.FORBIDDEN));
        }

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
            })
            .ToListAsync();

        var normalizedFileKey = string.IsNullOrWhiteSpace(fileKey) ? null : NormalizeFileKey(fileKey);
        var normalizedCoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim();

        var info = new AudioTrackRegistrationInfoDto
        {
            CreateTrackEndpoint = "POST /api/AudioTrack",
            Steps = new List<string>
            {
                "1. POST /api/File/audio/track — upload file MP3, lấy fileKey từ response",
                "2. POST /api/File/audio/cover — upload ảnh bìa (tuỳ chọn), lấy url từ response",
                "3. GET /api/File/audio/tracks — xem danh sách file đã upload và trạng thái đăng ký",
                "4. GET /api/File/audio/registration-info?fileKey=...&coverUrl=... — lấy body mẫu cho bước 5",
                "5. POST /api/AudioTrack — đăng ký bài hát với JSON body"
            },
            Categories = categories,
            Fields = new List<AudioTrackRegistrationFieldDto>
            {
                new()
                {
                    Name = "title",
                    Type = "string",
                    Required = true,
                    Description = "Tên bài hát hiển thị trên app",
                    Example = "Tiếng mưa tĩnh lặng"
                },
                new()
                {
                    Name = "artist",
                    Type = "string",
                    Required = false,
                    Description = "Tên nghệ sĩ hoặc nguồn âm thanh",
                    Example = "Âm thanh thiên nhiên"
                },
                new()
                {
                    Name = "studio",
                    Type = "string",
                    Required = false,
                    Description = "Studio hoặc nhãn phát hành",
                    Example = "HealthPath Audio"
                },
                new()
                {
                    Name = "categoryId",
                    Type = "guid",
                    Required = true,
                    Description = "ID danh mục — lấy từ trường categories trong response này",
                    Example = categories.FirstOrDefault()?.Id
                },
                new()
                {
                    Name = "durationSeconds",
                    Type = "int",
                    Required = true,
                    Description = "Thời lượng bài hát tính bằng giây (1–36000)",
                    Example = 330
                },
                new()
                {
                    Name = "fileUrl",
                    Type = "string",
                    Required = true,
                    Description = "Dùng fileKey từ POST /api/File/audio/track (KHÔNG dùng full URL)",
                    Example = normalizedFileKey ?? "audio/tracks/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.mp3"
                },
                new()
                {
                    Name = "coverUrl",
                    Type = "string",
                    Required = false,
                    Description = "URL ảnh bìa từ POST /api/File/audio/cover (dùng trường url)",
                    Example = normalizedCoverUrl ?? "https://pub-r2.example.com/audio/covers/xxxxxxxx.webp"
                },
                new()
                {
                    Name = "isPremium",
                    Type = "bool",
                    Required = false,
                    Description = "true nếu chỉ user Premium được nghe",
                    Example = false
                }
            },
            SuggestedBody = new CreateAudioTrackDto
            {
                Title = "Tên bài hát",
                Artist = "Tên nghệ sĩ",
                Studio = "HealthPath Audio",
                CategoryId = categories.FirstOrDefault()?.Id ?? Guid.Empty,
                DurationSeconds = 0,
                FileUrl = normalizedFileKey ?? string.Empty,
                CoverUrl = normalizedCoverUrl,
                IsPremium = false
            }
        };

        return Ok(ApiResponse<AudioTrackRegistrationInfoDto>.Ok(info));
    }

    [HttpPost("audio/track")]
    public async Task<IActionResult> UploadAudioTrack(IFormFile file)
    {
        var validationResult = ValidateFile(file, AllowedAudioExtensions, MaxAudioSize);
        if (validationResult != null) return BadRequest(validationResult);

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, "audio/tracks");

        var result = new FileUploadResultDto
        {
            Url = url,
            FileKey = ExtractKey(url),
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result));
    }

    [HttpPost("audio/cover")]
    public async Task<IActionResult> UploadAudioCover(IFormFile file)
    {
        var validationResult = ValidateFile(file, AllowedImageExtensions, MaxImageSize);
        if (validationResult != null) return BadRequest(validationResult);

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, "audio/covers");

        var result = new FileUploadResultDto
        {
            Url = url,
            FileKey = ExtractKey(url),
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrlOrKey)
    {
        if (string.IsNullOrEmpty(fileUrlOrKey))
        {
            return BadRequest(ApiResponse<object>.Fail("fileUrlOrKey is required", ErrorCode.VALIDATION_ERROR));
        }

        await _fileStorageService.DeleteAsync(fileUrlOrKey);
        return Ok(ApiResponse<object>.Ok(null!));
    }

    private ApiResponse<object>? ValidateFile(IFormFile? file, string[] allowedExtensions, long maxSize)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<object>.Fail("File is empty or not provided", ErrorCode.VALIDATION_ERROR);
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return ApiResponse<object>.Fail($"File type not allowed. Supported: {string.Join(", ", allowedExtensions)}", ErrorCode.FILE_TYPE_NOT_ALLOWED);
        }

        if (file.Length > maxSize)
        {
            return ApiResponse<object>.Fail($"File size exceeds limit of {maxSize / (1024 * 1024)}MB", ErrorCode.FILE_TOO_LARGE);
        }

        return null;
    }

    private string ExtractKey(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            var uri = new Uri(url);
            return uri.AbsolutePath.TrimStart('/');
        }
        return url.TrimStart('/');
    }

    private static string NormalizeFileKey(string fileUrlOrKey)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrKey))
        {
            return string.Empty;
        }

        var value = fileUrlOrKey.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            value = uri.AbsolutePath.TrimStart('/');
        }

        return value.TrimStart('/');
    }

    private async Task<bool> CanPerformAdminFileOpsAsync()
    {
        // Admin portal: JWT có claim IsAdmin=true (bảng admins, không có trong user_roles)
        if (User.IsAdminToken())
        {
            return true;
        }

        var userId = User.GetUserId();
        return await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId
                         && ur.Role.Name.ToLower() == "admin"
                         && ur.DeletedAt == null);
    }
}
