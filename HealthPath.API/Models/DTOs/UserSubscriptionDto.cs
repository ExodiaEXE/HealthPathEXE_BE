using System;

namespace HealthPath.API.Models.DTOs;

public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string BillingCycle { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentRef { get; set; }
    public bool IsActiveSubscription { get; set; }
}
