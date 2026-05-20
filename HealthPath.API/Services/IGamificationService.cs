using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IGamificationService
{
    Task ProcessCompletionAsync(Guid userRoutineId, Guid userId);
    Task<ApiResponse<UserStatsDto>> GetUserStatsAsync(Guid userId);
}
