using System;

namespace HealthPath.API.Services;

public class IapVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string PlatformTransactionId { get; set; } = null!;
    public string? OriginalTransactionId { get; set; }
    public DateTime PurchasedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    /// <summary>Google Play base plan id (e.g. healthpath-premium-monthly).</summary>
    public string? BasePlanId { get; set; }
    public string? LinkedPurchaseToken { get; set; }
    public bool AutoRenewEnabled { get; set; } = true;
    public string? SubscriptionState { get; set; }
}
