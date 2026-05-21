using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
}
