using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class CreateSubscriptionPlanDto
{
    [Required(ErrorMessage = "Tên gói là bắt buộc.")]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Mã code là bắt buộc.")]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá tháng không được âm.")]
    public decimal PriceMonthly { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá năm không được âm.")]
    public decimal PriceYearly { get; set; }

    [Required(ErrorMessage = "Loại tiền tệ là bắt buộc.")]
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public string Features { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? AppleProductId { get; set; }

    [MaxLength(100)]
    public string? GoogleProductId { get; set; }
}

public class UpdateSubscriptionPlanDto : CreateSubscriptionPlanDto
{
}
