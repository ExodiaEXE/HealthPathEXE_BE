using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        // Bất kỳ email nào (kể cả không phải đuôi trường) đều đăng ký được
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty; // Cái này để Mobile cầm đi làm mộc thông hành
    }

    public class VerifyOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordWithOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SocialLoginDto
    {
        [Required(ErrorMessage = "Token không được để trống.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provider không được để trống.")]
        public string Provider { get; set; } = string.Empty; // "google" or "facebook"
    }

    public class SocialLinkDto
    {
        [Required(ErrorMessage = "Token không được để trống.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provider không được để trống.")]
        public string Provider { get; set; } = string.Empty; // "google" or "facebook"
    }
}