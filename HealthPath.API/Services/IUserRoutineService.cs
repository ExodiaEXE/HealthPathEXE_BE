using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IUserRoutineService
{
    Task<ApiResponse<UserRoutineDto>> ScheduleRoutineAsync(CreateUserRoutineDto dto, Guid userId);
    Task<ApiResponse<UserRoutineDto>> StartRoutineAsync(Guid userRoutineId, Guid userId);
    Task<ApiResponse<UserRoutineDto>> CompleteRoutineAsync(Guid userRoutineId, UserRoutineStatusUpdateDto dto, Guid userId);
    Task<ApiResponse<UserRoutineDto>> FailRoutineAsync(Guid userRoutineId, Guid userId);
    Task<ApiResponse<PageResponse<UserRoutineDto>>> GetMyScheduleAsync(Guid userId, DateTime? date, int page, int pageSize);
}
