using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class VerifyReceiptRequestDto
{
    [Required]
    public string Platform { get; set; } = null!; // "GooglePlay" or "AppStore"

    [Required]
    public string ProductId { get; set; } = null!; // Product ID from Apple / Google

    [Required]
    public string PurchaseToken { get; set; } = null!; // Token from Google Play or Apple Receipt / JWS

    public string? TransactionId { get; set; } // Optional transaction ID from store

    public string BillingCycle { get; set; } = "monthly"; // "monthly" or "yearly"
}
