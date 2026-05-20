using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class UserRoutineService : IUserRoutineService
{
    private readonly HealthpathDbContext _context;

    public UserRoutineService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserRoutineDto>> ScheduleRoutineAsync(CreateUserRoutineDto dto, Guid userId)
    {
        var routine = await _context.Routines.FindAsync(dto.RoutineId);
        if (routine == null)
        {
            return ApiResponse<UserRoutineDto>.Fail("Routine not found", ErrorCode.ROUTINE_NOT_FOUND);
        }

        if (routine.IsPremium)
        {
            // Simple premium check: Check if user has an active UserSubscription
            var hasActiveSubscription = await _context.UserSubscriptions
                .AnyAsync(s => s.UserId == userId && s.Status == "active" && s.ExpiresAt > DateTime.UtcNow);

            if (!hasActiveSubscription)
            {
                return ApiResponse<UserRoutineDto>.Fail("Premium subscription is required for this routine", ErrorCode.PREMIUM_REQUIRED);
            }
        }

        var userRoutine = new UserRoutine
        {
            UserId = userId,
            RoutineId = dto.RoutineId,
            Status = "pending",
            ScheduledAt = dto.ScheduledAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserRoutines.Add(userRoutine);
        await _context.SaveChangesAsync();

        return ApiResponse<UserRoutineDto>.Ok(MapToDto(userRoutine), "Routine scheduled successfully");
    }

    public async Task<ApiResponse<UserRoutineDto>> StartRoutineAsync(Guid userRoutineId, Guid userId)
    {
        var userRoutine = await _context.UserRoutines.FirstOrDefaultAsync(ur => ur.Id == userRoutineId && ur.UserId == userId);
        if (userRoutine == null)
        {
            return ApiResponse<UserRoutineDto>.Fail("User routine not found", ErrorCode.USER_ROUTINE_NOT_FOUND);
        }

        if (userRoutine.Status != "pending")
        {
            return ApiResponse<UserRoutineDto>.Fail("Only pending routines can be started", ErrorCode.INVALID_STATE_TRANSITION);
        }

        userRoutine.Status = "in_progress";
        userRoutine.StartedAt = DateTime.UtcNow;
        userRoutine.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<UserRoutineDto>.Ok(MapToDto(userRoutine), "Routine started");
    }

    public async Task<ApiResponse<UserRoutineDto>> CompleteRoutineAsync(Guid userRoutineId, UserRoutineStatusUpdateDto dto, Guid userId)
    {
        var userRoutine = await _context.UserRoutines
            .Include(ur => ur.Routine)
            .FirstOrDefaultAsync(ur => ur.Id == userRoutineId && ur.UserId == userId);

        if (userRoutine == null)
        {
            return ApiResponse<UserRoutineDto>.Fail("User routine not found", ErrorCode.USER_ROUTINE_NOT_FOUND);
        }

        if (userRoutine.Status != "in_progress")
        {
            return ApiResponse<UserRoutineDto>.Fail("Only in_progress routines can be completed", ErrorCode.INVALID_STATE_TRANSITION);
        }

        userRoutine.Status = "completed";
        userRoutine.CompletedAt = DateTime.UtcNow;
        userRoutine.ActualDurationMinutes = dto.ActualDurationMinutes;
        userRoutine.ElapsedSeconds = dto.ElapsedSeconds ?? 0;
        userRoutine.UpdatedAt = DateTime.UtcNow;

        // Calculate score
        int baseScore = userRoutine.Routine?.DurationMinutes ?? 10;
        int difficultyMultiplier = userRoutine.Routine?.Difficulty switch
        {
            "hard" => 3,
            "medium" => 2,
            _ => 1
        };
        userRoutine.ScoreEarned = baseScore * difficultyMultiplier;

        // Update UserStats
        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (userStats == null)
        {
            userStats = new UserStats
            {
                UserId = userId,
                TotalScore = userRoutine.ScoreEarned,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserStats.Add(userStats);
        }
        else
        {
            userStats.TotalScore += userRoutine.ScoreEarned;
            userStats.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<UserRoutineDto>.Ok(MapToDto(userRoutine), "Routine completed");
    }

    public async Task<ApiResponse<UserRoutineDto>> FailRoutineAsync(Guid userRoutineId, Guid userId)
    {
        var userRoutine = await _context.UserRoutines.FirstOrDefaultAsync(ur => ur.Id == userRoutineId && ur.UserId == userId);
        if (userRoutine == null)
        {
            return ApiResponse<UserRoutineDto>.Fail("User routine not found", ErrorCode.USER_ROUTINE_NOT_FOUND);
        }

        if (userRoutine.Status != "in_progress" && userRoutine.Status != "pending")
        {
            return ApiResponse<UserRoutineDto>.Fail("Cannot fail this routine from current state", ErrorCode.INVALID_STATE_TRANSITION);
        }

        userRoutine.Status = "failed";
        userRoutine.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<UserRoutineDto>.Ok(MapToDto(userRoutine), "Routine marked as failed");
    }

    public async Task<ApiResponse<PageResponse<UserRoutineDto>>> GetMyScheduleAsync(Guid userId, DateTime? date, int page, int pageSize)
    {
        var query = _context.UserRoutines
            .Include(ur => ur.Routine)
            .Where(ur => ur.UserId == userId)
            .AsQueryable();

        if (date.HasValue)
        {
            var dateValue = date.Value.Date;
            query = query.Where(ur => ur.ScheduledAt != null && ur.ScheduledAt.Value.Date == dateValue);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(ur => ur.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ur => MapToDto(ur))
            .ToListAsync();

        var pageResponse = new PageResponse<UserRoutineDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return ApiResponse<PageResponse<UserRoutineDto>>.Ok(pageResponse);
    }

    private static UserRoutineDto MapToDto(UserRoutine entity)
    {
        return new UserRoutineDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RoutineId = entity.RoutineId,
            Status = entity.Status,
            ScheduledAt = entity.ScheduledAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ActualDurationMinutes = entity.ActualDurationMinutes,
            ScoreEarned = entity.ScoreEarned,
            ElapsedSeconds = entity.ElapsedSeconds,
            CreatedAt = entity.CreatedAt,
            Routine = entity.Routine != null ? new RoutineDto
            {
                Id = entity.Routine.Id,
                Title = entity.Routine.Title,
                Category = entity.Routine.Category,
                Difficulty = entity.Routine.Difficulty,
                DurationMinutes = entity.Routine.DurationMinutes,
                IsPremium = entity.Routine.IsPremium,
                ThumbnailUrl = entity.Routine.ThumbnailUrl
            } : null
        };
    }
}
