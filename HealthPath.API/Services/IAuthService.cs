using HealthPath.API.Models;

namespace HealthPath.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
    }
}