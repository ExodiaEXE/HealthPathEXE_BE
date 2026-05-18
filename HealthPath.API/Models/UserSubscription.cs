using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class UserSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public string Status { get; set; } = null!;

    public string BillingCycle { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? PaymentProvider { get; set; }

    public string? PaymentRef { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
