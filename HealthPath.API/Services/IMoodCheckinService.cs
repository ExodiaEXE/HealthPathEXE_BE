using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public interface IMoodCheckinService
    {
        Task<ApiResponse<MoodCheckinDto>> CreateCheckinAsync(Guid userId, CreateMoodCheckinDto dto);
        Task<ApiResponse<List<MoodCheckinDto>>> GetMyHistoryAsync(Guid userId);
        Task<ApiResponse<MoodCheckinDto>> GetByIdAsync(Guid id, Guid userId);
        Task<ApiResponse<MoodCheckinDto>> UpdateCheckinAsync(Guid id, Guid userId, UpdateMoodCheckinDto dto);
        Task<ApiResponse<object>> DeleteCheckinAsync(Guid id, Guid userId);
        Task<ApiResponse<MoodStatsDto>> GetStreakStatsAsync(Guid userId);
    }
}