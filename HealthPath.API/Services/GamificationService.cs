using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class GamificationService : IGamificationService
{
    private readonly HealthpathDbContext _context;

    public GamificationService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task ProcessCompletionAsync(Guid userRoutineId, Guid userId)
    {
        var userRoutine = await _context.UserRoutines.FirstOrDefaultAsync(ur => ur.Id == userRoutineId && ur.UserId == userId);
        if (userRoutine == null || userRoutine.Status != "completed")
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (userStats == null)
        {
            // First time completing a routine
            userStats = new UserStats
            {
                UserId = userId,
                StreakCurrent = 1,
                StreakBest = 1,
                StreakUpdatedDate = today,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserStats.Add(userStats);
        }
        else
        {
            if (userStats.StreakUpdatedDate == today)
            {
                // Already processed today, do nothing
            }
            else if (userStats.StreakUpdatedDate == yesterday)
            {
                // Consecutive day
                userStats.StreakCurrent++;
                userStats.StreakUpdatedDate = today;
                userStats.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Gap day, reset streak
                userStats.StreakCurrent = 1;
                userStats.StreakUpdatedDate = today;
                userStats.UpdatedAt = DateTime.UtcNow;
            }

            if (userStats.StreakCurrent > userStats.StreakBest)
            {
                userStats.StreakBest = userStats.StreakCurrent;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ApiResponse<UserStatsDto>> GetUserStatsAsync(Guid userId)
    {
        var stats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            return ApiResponse<UserStatsDto>.Ok(new UserStatsDto
            {
                StreakCurrent = 0,
                StreakBest = 0,
                StreakUpdatedDate = null
            }, "User stats not found, returned default values");
        }

        return ApiResponse<UserStatsDto>.Ok(new UserStatsDto
        {
            StreakCurrent = stats.StreakCurrent,
            StreakBest = stats.StreakBest,
            StreakUpdatedDate = stats.StreakUpdatedDate
        });
    }
}
