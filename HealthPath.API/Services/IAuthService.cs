using HealthPath.API.Models;
using HealthPath.API.Common;

namespace HealthPath.API.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> RegisterAsync(RegisterDto request);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto request);
    }
}