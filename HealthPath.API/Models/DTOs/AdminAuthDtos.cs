using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class AdminLoginDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    public string Password { get; set; } = null!;
}

public class CreateAdminDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải dài ít nhất 6 ký tự.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vai trò là bắt buộc.")]
    [RegularExpression("^(SuperAdmin|Moderator)$", ErrorMessage = "Vai trò phải là 'SuperAdmin' hoặc 'Moderator'.")]
    public string Role { get; set; } = "Moderator";
}

public class AdminAuthResponseDto
{
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
}
