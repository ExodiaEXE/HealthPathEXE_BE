using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class AdminUserSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public bool HasPremiumAccess { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public UserSubscriptionDto? ActiveSubscription { get; set; }
}

public class AdminCreateUserDto
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
    public string Password { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    public string? Phone { get; set; }
}
