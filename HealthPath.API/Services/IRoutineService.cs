using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IRoutineService
{
    Task<ApiResponse<PageResponse<RoutineDto>>> GetRoutinesAsync(string? category, string? difficulty, int page, int pageSize);
    Task<ApiResponse<RoutineDto>> GetRoutineByIdAsync(Guid id);
    Task<ApiResponse<RoutineDto>> CreateRoutineAsync(
        CreateRoutineDto dto,
        Guid? createdBy,
        bool isSystem = false);
}
