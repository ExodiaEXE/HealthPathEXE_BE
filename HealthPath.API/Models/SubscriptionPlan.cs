using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class SubscriptionPlan
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

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
