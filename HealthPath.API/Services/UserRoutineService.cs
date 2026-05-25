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
    private readonly IGamificationService _gamificationService;

    public UserRoutineService(HealthpathDbContext context, IGamificationService gamificationService)
    {
        _context = context;
        _gamificationService = gamificationService;
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

        await _context.SaveChangesAsync();

        // Trigger gamification (streak logic)
        await _gamificationService.ProcessCompletionAsync(userRoutine.Id, userId);

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

    public async Task<ApiResponse<RecurringTemplateDto>> CreateRecurringTemplateAsync(CreateRecurringTemplateDto dto, Guid userId)
    {
        var routine = await _context.Routines.FindAsync(dto.RoutineId);
        if (routine == null)
        {
            return ApiResponse<RecurringTemplateDto>.Fail("Routine not found", ErrorCode.ROUTINE_NOT_FOUND);
        }

        if (routine.IsPremium)
        {
            var hasActiveSubscription = await _context.UserSubscriptions
                .AnyAsync(s => s.UserId == userId && s.Status == "active" && s.ExpiresAt > DateTime.UtcNow);

            if (!hasActiveSubscription)
            {
                return ApiResponse<RecurringTemplateDto>.Fail("Premium subscription is required for this routine", ErrorCode.PREMIUM_REQUIRED);
            }
        }

        if (dto.DaysOfWeek == null || dto.DaysOfWeek.Count == 0)
        {
            return ApiResponse<RecurringTemplateDto>.Fail("DaysOfWeek cannot be empty", ErrorCode.VALIDATION_ERROR);
        }

        if (dto.DaysOfWeek.Any(d => d < 1 || d > 7))
        {
            return ApiResponse<RecurringTemplateDto>.Fail("Days of week must be between 1 (Monday) and 7 (Sunday)", ErrorCode.VALIDATION_ERROR);
        }

        if (!TimeOnly.TryParse(dto.ScheduledTime, out var scheduledTime))
        {
            return ApiResponse<RecurringTemplateDto>.Fail("Invalid ScheduledTime format. Expected HH:mm:ss", ErrorCode.VALIDATION_ERROR);
        }

        var template = new RecurringTemplate
        {
            UserId = userId,
            RoutineId = dto.RoutineId,
            DaysOfWeek = System.Text.Json.JsonSerializer.Serialize(dto.DaysOfWeek),
            ScheduledTime = scheduledTime,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecurringTemplates.Add(template);
        await _context.SaveChangesAsync();

        var resultDto = MapToTemplateDto(template);
        resultDto.Routine = new RoutineDto
        {
            Id = routine.Id,
            Title = routine.Title,
            Category = routine.Category,
            Difficulty = routine.Difficulty,
            DurationMinutes = routine.DurationMinutes,
            IsPremium = routine.IsPremium,
            ThumbnailUrl = routine.ThumbnailUrl
        };

        return ApiResponse<RecurringTemplateDto>.Ok(resultDto, "Recurring template created successfully");
    }

    public async Task<ApiResponse<System.Collections.Generic.List<RecurringTemplateDto>>> GetMyRecurringTemplatesAsync(Guid userId)
    {
        var templates = await _context.RecurringTemplates
            .Include(t => t.Routine)
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .ToListAsync();

        var dtos = templates.Select(MapToTemplateDto).ToList();
        return ApiResponse<System.Collections.Generic.List<RecurringTemplateDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<object>> DeleteRecurringTemplateAsync(Guid templateId, Guid userId)
    {
        var template = await _context.RecurringTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId && t.DeletedAt == null);

        if (template == null)
        {
            return ApiResponse<object>.Fail("Recurring template not found", ErrorCode.USER_ROUTINE_NOT_FOUND);
        }

        template.IsActive = false;
        template.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<object>.Ok(new object(), "Recurring template deleted successfully");
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

    private static RecurringTemplateDto MapToTemplateDto(RecurringTemplate entity)
    {
        System.Collections.Generic.List<int> daysList;
        try
        {
            daysList = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<int>>(entity.DaysOfWeek) 
                       ?? new System.Collections.Generic.List<int>();
        }
        catch
        {
            daysList = new System.Collections.Generic.List<int>();
        }

        return new RecurringTemplateDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RoutineId = entity.RoutineId,
            DaysOfWeek = daysList,
            ScheduledTime = entity.ScheduledTime.ToString("HH:mm:ss"),
            IsActive = entity.IsActive,
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

