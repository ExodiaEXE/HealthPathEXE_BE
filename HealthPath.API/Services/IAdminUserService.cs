using System;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IAdminUserService
{
    Task<ApiResponse<PageResponse<AdminUserSummaryDto>>> GetUsersPagedAsync(string? search, bool? onlyPremium, int page, int pageSize);
    Task<ApiResponse<AdminUserDetailDto>> GetUserDetailAsync(Guid id);
    Task<ApiResponse<AdminUserSummaryDto>> CreateUserAsync(AdminCreateUserDto request);
    Task<ApiResponse<bool>> ToggleUserActiveAsync(Guid id);
}
