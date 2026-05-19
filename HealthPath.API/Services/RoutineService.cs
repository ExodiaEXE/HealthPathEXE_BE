using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class RoutineService : IRoutineService
{
    private readonly HealthpathDbContext _context;

    public RoutineService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PageResponse<RoutineDto>>> GetRoutinesAsync(string? category, string? difficulty, int page, int pageSize)
    {
        var query = _context.Routines.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.Category == category);
        }

        if (!string.IsNullOrEmpty(difficulty))
        {
            query = query.Where(r => r.Difficulty == difficulty);
        }

        var totalItems = await query.CountAsync();

        var routines = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoutineDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Category = r.Category,
                Difficulty = r.Difficulty,
                DurationMinutes = r.DurationMinutes,
                IsPremium = r.IsPremium,
                ThumbnailUrl = r.ThumbnailUrl,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        var pageResponse = new PageResponse<RoutineDto>
        {
            Items = routines,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return ApiResponse<PageResponse<RoutineDto>>.Ok(pageResponse);
    }

    public async Task<ApiResponse<RoutineDto>> GetRoutineByIdAsync(Guid id)
    {
        var routine = await _context.Routines.FindAsync(id);
        if (routine == null)
        {
            return ApiResponse<RoutineDto>.Fail("Routine not found.", ErrorCode.ROUTINE_NOT_FOUND);
        }

        var dto = new RoutineDto
        {
            Id = routine.Id,
            Title = routine.Title,
            Description = routine.Description,
            Category = routine.Category,
            Difficulty = routine.Difficulty,
            DurationMinutes = routine.DurationMinutes,
            IsPremium = routine.IsPremium,
            ThumbnailUrl = routine.ThumbnailUrl,
            CreatedAt = routine.CreatedAt
        };

        return ApiResponse<RoutineDto>.Ok(dto);
    }

    public async Task<ApiResponse<RoutineDto>> CreateRoutineAsync(CreateRoutineDto dto, Guid currentUserId)
    {
        var routine = new Routine
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Difficulty = dto.Difficulty,
            DurationMinutes = dto.DurationMinutes,
            IsPremium = dto.IsPremium,
            ThumbnailUrl = dto.ThumbnailUrl,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsSystem = false // Assuming user-created routines are not system routines by default
        };

        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        var resultDto = new RoutineDto
        {
            Id = routine.Id,
            Title = routine.Title,
            Description = routine.Description,
            Category = routine.Category,
            Difficulty = routine.Difficulty,
            DurationMinutes = routine.DurationMinutes,
            IsPremium = routine.IsPremium,
            ThumbnailUrl = routine.ThumbnailUrl,
            CreatedAt = routine.CreatedAt
        };

        return ApiResponse<RoutineDto>.Ok(resultDto, "Routine created successfully");
    }
}
