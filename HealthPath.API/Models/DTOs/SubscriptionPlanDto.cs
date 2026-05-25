using System;

namespace HealthPath.API.Models.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public string Currency { get; set; } = null!;
    public string Features { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? AppleProductId { get; set; }
    public string? GoogleProductId { get; set; }
}
