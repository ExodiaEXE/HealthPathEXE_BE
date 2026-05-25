using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly HealthpathDbContext _context;
    private readonly IIapVerificationService _iapVerificationService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        HealthpathDbContext context,
        IIapVerificationService iapVerificationService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _iapVerificationService = iapVerificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SubscriptionPlanDto>>> GetPlansAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive && p.DeletedAt == null)
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                Currency = p.Currency,
                Features = p.Features,
                IsActive = p.IsActive,
                AppleProductId = p.AppleProductId,
                GoogleProductId = p.GoogleProductId
            })
            .ToListAsync();

        return ApiResponse<List<SubscriptionPlanDto>>.Ok(plans, "Lấy danh sách gói thành công.");
    }

    public async Task<ApiResponse<UserSubscriptionDto?>> GetCurrentSubscriptionAsync(Guid userId)
    {
        var sub = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        if (sub == null)
        {
            return ApiResponse<UserSubscriptionDto?>.Ok(null, "Không có gói đăng ký hoạt động.");
        }

        // A subscription is active if status is "active" and (ExpiresAt is null OR expires in the future)
        bool isActive = sub.Status == "active" && (sub.ExpiresAt == null || sub.ExpiresAt > DateTime.UtcNow);

        var dto = new UserSubscriptionDto
        {
            Id = sub.Id,
            UserId = sub.UserId,
            PlanId = sub.PlanId,
            PlanName = sub.Plan.Name,
            Status = sub.Status,
            BillingCycle = sub.BillingCycle,
            StartedAt = sub.StartedAt,
            ExpiresAt = sub.ExpiresAt,
            CancelledAt = sub.CancelledAt,
            PaymentProvider = sub.PaymentProvider,
            PaymentRef = sub.PaymentRef,
            IsActiveSubscription = isActive
        };

        return ApiResponse<UserSubscriptionDto?>.Ok(dto, "Lấy trạng thái gói thành công.");
    }

    public async Task<ApiResponse<List<TransactionDto>>> GetMyTransactionsAsync(Guid userId)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Plan)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.PurchasedAt)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                PlanId = t.PlanId,
                PlanName = t.Plan.Name,
                Platform = t.Platform,
                PlatformTransactionId = t.PlatformTransactionId,
                OriginalTransactionId = t.OriginalTransactionId,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                PurchasedAt = t.PurchasedAt,
                ExpiresAt = t.ExpiresAt,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<TransactionDto>>.Ok(transactions, "Lấy lịch sử giao dịch thành công.");
    }

    public async Task<ApiResponse<UserSubscriptionDto>> VerifyAndFulfillPurchaseAsync(Guid userId, VerifyReceiptRequestDto request)
    {
        _logger.LogInformation("Processing purchase for User {UserId}, Platform {Platform}, Product {ProductId}", userId, request.Platform, request.ProductId);

        // 1. Find corresponding subscription plan based on Store Product ID
        SubscriptionPlan? plan = null;
        if (request.Platform.Equals("GooglePlay", StringComparison.OrdinalIgnoreCase) || 
            request.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.GoogleProductId == request.ProductId && p.DeletedAt == null);
        }
        else if (request.Platform.Equals("AppStore", StringComparison.OrdinalIgnoreCase) || 
                 request.Platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
        {
            plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.AppleProductId == request.ProductId && p.DeletedAt == null);
        }

        // Fallback: search by Plan Code
        if (plan == null)
        {
            plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == request.ProductId && p.DeletedAt == null);
        }

        if (plan == null)
        {
            _logger.LogError("Subscription plan for product ID {ProductId} not found.", request.ProductId);
            return ApiResponse<UserSubscriptionDto>.Fail("Không tìm thấy gói subscription tương ứng.", ErrorCode.SUBSCRIPTION_PLAN_NOT_FOUND);
        }

        // 2. Perform receipt/token verification
        IapVerificationResult verificationResult;
        string platformCanonical = "";

        if (request.Platform.Equals("GooglePlay", StringComparison.OrdinalIgnoreCase) || 
            request.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            platformCanonical = "GooglePlay";
            verificationResult = await _iapVerificationService.VerifyAndroidPurchaseAsync(request.ProductId, request.PurchaseToken);
        }
        else if (request.Platform.Equals("AppStore", StringComparison.OrdinalIgnoreCase) || 
                 request.Platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
        {
            platformCanonical = "AppStore";
            verificationResult = await _iapVerificationService.VerifyIosPurchaseAsync(request.ProductId, request.PurchaseToken);
        }
        else
        {
            return ApiResponse<UserSubscriptionDto>.Fail("Platform không được hỗ trợ. Sử dụng 'GooglePlay' hoặc 'AppStore'.", ErrorCode.VALIDATION_ERROR);
        }

        if (!verificationResult.IsValid)
        {
            _logger.LogError("IAP verification failed: {Error}", verificationResult.ErrorMessage);
            return ApiResponse<UserSubscriptionDto>.Fail($"Xác thực thanh toán thất bại: {verificationResult.ErrorMessage}", ErrorCode.IAP_VERIFICATION_FAILED);
        }

        // 3. Fulfill the purchase: save Transaction & update/create UserSubscription
        
        // Check duplicate transaction
        var existingTx = await _context.Transactions
            .FirstOrDefaultAsync(t => t.PlatformTransactionId == verificationResult.PlatformTransactionId);

        if (existingTx == null)
        {
            var newTx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Platform = platformCanonical,
                PlatformTransactionId = verificationResult.PlatformTransactionId,
                OriginalTransactionId = verificationResult.OriginalTransactionId,
                PurchaseToken = request.PurchaseToken,
                Status = "Success",
                Amount = verificationResult.Amount > 0 ? verificationResult.Amount : (request.BillingCycle == "yearly" ? plan.PriceYearly : plan.PriceMonthly),
                Currency = verificationResult.Currency ?? "VND",
                PurchasedAt = verificationResult.PurchasedAt,
                ExpiresAt = verificationResult.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(newTx);
        }

        // Retrieve or create UserSubscription
        var userSub = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeletedAt == null);

        DateTime expireDate = verificationResult.ExpiresAt ?? 
                             verificationResult.PurchasedAt.AddMonths(request.BillingCycle == "yearly" ? 12 : 1);

        if (userSub == null)
        {
            userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Status = "active",
                BillingCycle = request.BillingCycle,
                StartedAt = verificationResult.PurchasedAt,
                ExpiresAt = expireDate,
                PaymentProvider = platformCanonical,
                PaymentRef = verificationResult.PlatformTransactionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(userSub);
        }
        else
        {
            userSub.PlanId = plan.Id;
            userSub.Status = "active";
            userSub.BillingCycle = request.BillingCycle;
            userSub.StartedAt = verificationResult.PurchasedAt;
            userSub.ExpiresAt = expireDate;
            userSub.PaymentProvider = platformCanonical;
            userSub.PaymentRef = verificationResult.PlatformTransactionId;
            userSub.UpdatedAt = DateTime.UtcNow;
            userSub.CancelledAt = null; // Reset cancelled state if purchased again/renewed
        }

        await _context.SaveChangesAsync();

        var subDto = new UserSubscriptionDto
        {
            Id = userSub.Id,
            UserId = userSub.UserId,
            PlanId = userSub.PlanId,
            PlanName = plan.Name,
            Status = userSub.Status,
            BillingCycle = userSub.BillingCycle,
            StartedAt = userSub.StartedAt,
            ExpiresAt = userSub.ExpiresAt,
            CancelledAt = userSub.CancelledAt,
            PaymentProvider = userSub.PaymentProvider,
            PaymentRef = userSub.PaymentRef,
            IsActiveSubscription = true
        };

        return ApiResponse<UserSubscriptionDto>.Ok(subDto, "Kích hoạt và cập nhật gói premium thành công!");
    }

    public async Task<ApiResponse<bool>> ProcessServerNotificationAsync(string platform, JsonElement payload)
    {
        _logger.LogInformation("Processing Server-To-Server Notification for platform {Platform}", platform);

        if (platform.Equals("AppStore", StringComparison.OrdinalIgnoreCase))
        {
            // Apple S2S Notification processing
            try
            {
                // In modern Apple S2S notifications: JWS payload structure containing "signedPayload"
                if (payload.TryGetProperty("signedPayload", out var signedPayloadElement))
                {
                    string signedPayload = signedPayloadElement.GetString()!;
                    _logger.LogInformation("Processing Apple App Store signedPayload JWS notification.");
                    
                    // Decode JWS (in real applications, utilize JWT library or parse it)
                    // Let's decode the payload's body. JWS has 3 parts: header.payload.signature
                    string[] parts = signedPayload.Split('.');
                    if (parts.Length == 3)
                    {
                        string decodedPayloadStr = Base64UrlDecode(parts[1]);
                        using var doc = JsonDocument.Parse(decodedPayloadStr);
                        var root = doc.RootElement;
                        
                        string notificationType = root.GetProperty("notificationType").GetString()!;
                        _logger.LogInformation("Apple notification type: {Type}", notificationType);

                        if (root.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("signedTransactionInfo", out var txInfoElement))
                        {
                            string signedTxInfo = txInfoElement.GetString()!;
                            string[] txParts = signedTxInfo.Split('.');
                            if (txParts.Length == 3)
                            {
                                string decodedTxStr = Base64UrlDecode(txParts[1]);
                                using var txDoc = JsonDocument.Parse(decodedTxStr);
                                var txRoot = txDoc.RootElement;

                                string originalTransactionId = txRoot.GetProperty("originalTransactionId").GetString()!;
                                string transactionId = txRoot.GetProperty("transactionId").GetString()!;
                                string productId = txRoot.GetProperty("productId").GetString()!;
                                long expiresDateMs = txRoot.GetProperty("expiresDate").GetInt64();
                                DateTime expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresDateMs).UtcDateTime;

                                _logger.LogInformation("Transaction info extracted: OriginalID: {Orig}, ProductID: {Prod}, ExpiresAt: {Exp}", originalTransactionId, productId, expiresAt);

                                // Find UserSubscription that maps to this transaction
                                var userSub = await _context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => (s.PaymentRef == originalTransactionId || s.PaymentRef == transactionId) && s.DeletedAt == null);

                                if (userSub != null)
                                {
                                    if (notificationType == "DID_RENEW" || notificationType == "SUBSCRIBED")
                                    {
                                        userSub.Status = "active";
                                        userSub.ExpiresAt = expiresAt;
                                        userSub.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else if (notificationType == "DID_FAIL_TO_RENEW" || notificationType == "EXPIRED")
                                    {
                                        userSub.Status = "expired";
                                        userSub.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else if (notificationType == "REVOKE") // Refunded
                                    {
                                        userSub.Status = "refunded";
                                        userSub.CancelledAt = DateTime.UtcNow;
                                        userSub.UpdatedAt = DateTime.UtcNow;
                                    }

                                    await _context.SaveChangesAsync();
                                    _logger.LogInformation("Successfully updated user subscription status for original transaction ID: {OriginalTxId}", originalTransactionId);
                                }
                            }
                        }
                    }
                }
                return ApiResponse<bool>.Ok(true, "Apple Webhook processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Apple Webhook notification");
                return ApiResponse<bool>.Fail($"Lỗi xử lý webhook Apple: {ex.Message}", ErrorCode.INTERNAL_ERROR);
            }
        }
        else if (platform.Equals("GooglePlay", StringComparison.OrdinalIgnoreCase))
        {
            // Google RTDN processing
            try
            {
                // In Google Play: message data is Base64 encoded
                if (payload.TryGetProperty("message", out var messageProp) && messageProp.TryGetProperty("data", out var dataProp))
                {
                    string base64Data = dataProp.GetString()!;
                    string decodedData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                    
                    using var doc = JsonDocument.Parse(decodedData);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("subscriptionNotification", out var subNotif))
                    {
                        int notificationType = subNotif.GetProperty("notificationType").GetInt32();
                        string purchaseToken = subNotif.GetProperty("purchaseToken").GetString()!;
                        string subscriptionId = subNotif.GetProperty("subscriptionId").GetString()!;

                        _logger.LogInformation("Google notification type: {Type}, Token: {Token}", notificationType, purchaseToken);

                        // Find corresponding subscription
                        var userSub = await _context.UserSubscriptions
                            .FirstOrDefaultAsync(s => s.PaymentRef == purchaseToken && s.DeletedAt == null);

                        if (userSub != null)
                        {
                            // Types: 2 (Renewed), 3 (Cancelled), 5 (Expired), 12 (Revoked/Refunded)
                            if (notificationType == 2) // RENEWED
                            {
                                userSub.Status = "active";
                                // For real renew, backend should query Google Publisher API to find new expires_at,
                                // but for webhook processing, we can add a month as default or just log it.
                                userSub.ExpiresAt = userSub.ExpiresAt?.AddMonths(userSub.BillingCycle == "yearly" ? 12 : 1) ?? DateTime.UtcNow.AddMonths(1);
                                userSub.UpdatedAt = DateTime.UtcNow;
                            }
                            else if (notificationType == 3) // CANCELED
                            {
                                userSub.CancelledAt = DateTime.UtcNow;
                                userSub.UpdatedAt = DateTime.UtcNow;
                            }
                            else if (notificationType == 5 || notificationType == 6) // EXPIRED or ON_HOLD
                            {
                                userSub.Status = "expired";
                                userSub.UpdatedAt = DateTime.UtcNow;
                            }
                            else if (notificationType == 12) // REVOKED (Refunded)
                            {
                                userSub.Status = "refunded";
                                userSub.CancelledAt = DateTime.UtcNow;
                                userSub.UpdatedAt = DateTime.UtcNow;
                            }

                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Successfully updated user subscription status for Google purchase token.");
                        }
                    }
                }
                return ApiResponse<bool>.Ok(true, "Google Webhook processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google Webhook notification");
                return ApiResponse<bool>.Fail($"Lỗi xử lý webhook Google: {ex.Message}", ErrorCode.INTERNAL_ERROR);
            }
        }

        return ApiResponse<bool>.Fail("Platform không hợp lệ.", ErrorCode.VALIDATION_ERROR);
    }

    private static string Base64UrlDecode(string input)
    {
        string incoming = input.Replace('_', '/').Replace('-', '+');
        switch (incoming.Length % 4)
        {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        byte[] bytes = Convert.FromBase64String(incoming);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
