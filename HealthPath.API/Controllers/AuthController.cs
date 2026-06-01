using HealthPath.API.Models;
using HealthPath.API.Services;
using HealthPath.API.Common;
using HealthPath.API.Extensions; // Bổ sung để dùng GetUserId()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Bổ sung để dùng [Authorize]

namespace HealthPath.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success) return Unauthorized(result);
            return Ok(result);
        }

        [HttpPost("verify-register-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpDto request)
        {
            var result = await _authService.VerifyRegisterOtpAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("reset-password-with-otp")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto request)
        {
            var result = await _authService.ResetPasswordWithOtpAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto request)
        {
            var result = await _authService.SocialLoginAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("link-social")]
        public async Task<IActionResult> LinkSocial([FromBody] SocialLinkDto request)
        {
            var userId = User.GetUserId();
            var result = await _authService.LinkSocialAccountAsync(userId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("unlink-social")]
        public async Task<IActionResult> UnlinkSocial([FromQuery] string provider)
        {
            var userId = User.GetUserId();
            var result = await _authService.UnlinkSocialAccountAsync(userId, provider);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}