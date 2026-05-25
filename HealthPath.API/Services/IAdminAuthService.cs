using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IAdminAuthService
{
    Task<ApiResponse<AdminAuthResponseDto>> LoginAsync(AdminLoginDto request);
    Task<ApiResponse<bool>> CreateAdminAsync(CreateAdminDto request);
}
