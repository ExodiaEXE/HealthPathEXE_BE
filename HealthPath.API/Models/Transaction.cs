using System;

namespace HealthPath.API.Models;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public string Platform { get; set; } = null!; // "GooglePlay" or "AppStore"

    public string PlatformTransactionId { get; set; } = null!;

    public string? OriginalTransactionId { get; set; }

    public string PurchaseToken { get; set; } = null!;

    public string Status { get; set; } = null!; // "Success", "Refunded", "Cancelled", "Pending"

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime PurchasedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
