using HealthPath.API.Models;
using HealthPath.API.Common;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> RegisterAsync(RegisterDto request);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto request);

        // Thêm 3 phương thức mới xử lý luồng OTP
        Task<ApiResponse<object>> VerifyRegisterOtpAsync(VerifyOtpDto request);
        Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto request);
        Task<ApiResponse<object>> ResetPasswordWithOtpAsync(ResetPasswordWithOtpDto request);
    }
}