using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services
{
    public interface IMoodCheckinService
    {
        Task<ApiResponse<MoodCheckinDto>> CreateCheckinAsync(Guid userId, CreateMoodCheckinDto dto);
        Task<ApiResponse<List<MoodCheckinDto>>> GetMyHistoryAsync(Guid userId);
    }
}