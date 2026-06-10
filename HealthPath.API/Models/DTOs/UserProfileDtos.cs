using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class UpdateUserProfileDto
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }
}
