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
    private const string GoogleSubscriptionProductId = "healthpath_subscription";

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
            .ToListAsync();

        var distinctTransactions = transactions
            .GroupBy(t => t.PlatformTransactionId)
            .Select(g => g.OrderByDescending(t => t.UpdatedAt).First())
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
            .ToList();

        return ApiResponse<List<TransactionDto>>.Ok(distinctTransactions, "Lấy lịch sử giao dịch thành công.");
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

        // Fallback: subscription product ID + billing cycle (Play Console base plans)
        if (plan == null &&
            string.Equals(request.ProductId, GoogleSubscriptionProductId, StringComparison.OrdinalIgnoreCase))
        {
            var planCode = "premium_monthly";
            plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == planCode && p.DeletedAt == null && p.IsActive);
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

        var billingCycle = ResolveBillingCycle(request.BillingCycle, verificationResult.BasePlanId);
        if (platformCanonical == "GooglePlay")
        {
            var resolvedPlan = await ResolveGooglePlanAsync(
                request.ProductId,
                billingCycle,
                verificationResult.BasePlanId);
            if (resolvedPlan != null)
            {
                plan = resolvedPlan;
            }
        }

        // 3. Fulfill the purchase: save Transaction & update/create UserSubscription
        try
        {
            await UpsertVerifiedTransactionAsync(
                userId,
                plan,
                platformCanonical,
                request.PurchaseToken,
                billingCycle,
                verificationResult);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<UserSubscriptionDto>.Fail(ex.Message, ErrorCode.VALIDATION_ERROR);
        }

        var userSub = await ApplyVerifiedUserSubscriptionAsync(
            userId,
            plan,
            platformCanonical,
            request.PurchaseToken,
            billingCycle,
            verificationResult);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(
                ex,
                "Duplicate IAP transaction for user {UserId}, order {OrderId} — retrying idempotent verify.",
                userId,
                verificationResult.PlatformTransactionId);

            _context.ChangeTracker.Clear();

            await UpsertVerifiedTransactionAsync(
                userId,
                plan,
                platformCanonical,
                request.PurchaseToken,
                billingCycle,
                verificationResult);

            userSub = await ApplyVerifiedUserSubscriptionAsync(
                userId,
                plan,
                platformCanonical,
                request.PurchaseToken,
                billingCycle,
                verificationResult);

            await _context.SaveChangesAsync();
        }

        var subDto = MapUserSubscriptionDto(userSub, plan);
        return ApiResponse<UserSubscriptionDto>.Ok(subDto, "Kích hoạt và cập nhật gói premium thành công!");
    }

    private async Task<UserSubscription> ApplyVerifiedUserSubscriptionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string platformCanonical,
        string purchaseToken,
        string billingCycle,
        IapVerificationResult verificationResult)
    {
        var userSub = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeletedAt == null);

        var expireDate = verificationResult.ExpiresAt ??
            verificationResult.PurchasedAt.AddMonths(billingCycle == "yearly" ? 12 : 1);

        if (userSub == null)
        {
            userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Status = "active",
                BillingCycle = billingCycle,
                StartedAt = verificationResult.PurchasedAt,
                ExpiresAt = expireDate,
                PaymentProvider = platformCanonical,
                PaymentRef = ResolvePaymentRef(platformCanonical, purchaseToken, verificationResult),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(userSub);
        }
        else
        {
            userSub.PlanId = plan.Id;
            userSub.Status = "active";
            userSub.BillingCycle = billingCycle;
            userSub.ExpiresAt = expireDate;
            userSub.PaymentProvider = platformCanonical;
            userSub.PaymentRef = ResolvePaymentRef(platformCanonical, purchaseToken, verificationResult);
            userSub.UpdatedAt = DateTime.UtcNow;
            ApplyCancellationState(userSub, verificationResult);
        }

        return userSub;
    }

    private static void ApplyCancellationState(
        UserSubscription userSub,
        IapVerificationResult verificationResult)
    {
        var state = verificationResult.SubscriptionState ?? string.Empty;
        var cancelledOnPlay = !verificationResult.AutoRenewEnabled
            || state.Contains("CANCELED", StringComparison.OrdinalIgnoreCase);

        if (cancelledOnPlay)
        {
            userSub.CancelledAt ??= DateTime.UtcNow;
            return;
        }

        userSub.CancelledAt = null;
    }

    private async Task UpsertVerifiedTransactionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string platform,
        string purchaseToken,
        string billingCycle,
        IapVerificationResult verification)
    {
        var platformTransactionId = verification.PlatformTransactionId;
        var existingTx = await _context.Transactions.FirstOrDefaultAsync(t =>
            t.PlatformTransactionId == platformTransactionId
            || (t.Platform == platform && t.PurchaseToken == purchaseToken));

        var amount = verification.Amount > 0
            ? verification.Amount
            : billingCycle == "yearly" ? plan.PriceYearly : plan.PriceMonthly;

        if (existingTx != null)
        {
            if (existingTx.UserId != userId)
            {
                throw new InvalidOperationException(
                    "Giao dịch Google Play đã được liên kết với tài khoản khác.");
            }

            existingTx.PlanId = plan.Id;
            existingTx.PurchaseToken = purchaseToken;
            existingTx.Status = "Success";
            existingTx.Amount = amount;
            existingTx.Currency = verification.Currency ?? "VND";
            existingTx.PurchasedAt = verification.PurchasedAt;
            existingTx.ExpiresAt = verification.ExpiresAt;
            existingTx.OriginalTransactionId = verification.OriginalTransactionId;
            existingTx.UpdatedAt = DateTime.UtcNow;

            if (!string.Equals(existingTx.PlatformTransactionId, platformTransactionId, StringComparison.Ordinal)
                && !await _context.Transactions.AnyAsync(t =>
                    t.Id != existingTx.Id && t.PlatformTransactionId == platformTransactionId))
            {
                existingTx.PlatformTransactionId = platformTransactionId;
            }

            return;
        }

        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            Platform = platform,
            PlatformTransactionId = platformTransactionId,
            OriginalTransactionId = verification.OriginalTransactionId,
            PurchaseToken = purchaseToken,
            Status = "Success",
            Amount = amount,
            Currency = verification.Currency ?? "VND",
            PurchasedAt = verification.PurchasedAt,
            ExpiresAt = verification.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static UserSubscriptionDto MapUserSubscriptionDto(UserSubscription userSub, SubscriptionPlan plan)
    {
        var isActive = userSub.Status == "active"
            && (userSub.ExpiresAt == null || userSub.ExpiresAt > DateTime.UtcNow);

        return new UserSubscriptionDto
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
            IsActiveSubscription = isActive
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505", StringComparison.Ordinal)
               || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
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

                        // Find user via stored purchase token (PaymentRef or Transactions)
                        var userSub = await FindGoogleSubscriptionByPurchaseTokenAsync(purchaseToken);

                        if (userSub != null)
                        {
                            await ApplyGoogleSubscriptionNotificationAsync(
                                userSub,
                                notificationType,
                                subscriptionId,
                                purchaseToken);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Google RTDN: no user subscription for token {Token}, type {Type}",
                                purchaseToken,
                                notificationType);
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

    private static string ResolvePaymentRef(
        string platform,
        string purchaseToken,
        IapVerificationResult verification)
    {
        if (platform.Equals("GooglePlay", StringComparison.OrdinalIgnoreCase))
        {
            return purchaseToken;
        }

        return verification.OriginalTransactionId ?? verification.PlatformTransactionId;
    }

    private async Task<UserSubscription?> FindGoogleSubscriptionByPurchaseTokenAsync(string purchaseToken)
    {
        var byRef = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s =>
                s.PaymentRef == purchaseToken
                && s.PaymentProvider == "GooglePlay"
                && s.DeletedAt == null);
        if (byRef != null)
        {
            return byRef;
        }

        var tx = await _context.Transactions
            .Where(t => t.PurchaseToken == purchaseToken && t.Platform == "GooglePlay")
            .OrderByDescending(t => t.PurchasedAt)
            .FirstOrDefaultAsync();

        if (tx == null)
        {
            return null;
        }

        return await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == tx.UserId && s.DeletedAt == null);
    }

    private async Task ApplyGoogleSubscriptionNotificationAsync(
        UserSubscription userSub,
        int notificationType,
        string subscriptionId,
        string purchaseToken)
    {
        // 1 RECOVERED, 2 RENEWED, 4 PURCHASED, 7 RESTARTED — re-verify and activate
        if (notificationType is 1 or 2 or 4 or 7)
        {
            var verification = await _iapVerificationService
                .VerifyAndroidPurchaseAsync(subscriptionId, purchaseToken);
            if (verification.IsValid)
            {
                var billingCycle = ResolveBillingCycle(userSub.BillingCycle, verification.BasePlanId);
                var plan = await ResolveGooglePlanAsync(subscriptionId, billingCycle, verification.BasePlanId);
                if (plan != null)
                {
                    userSub.PlanId = plan.Id;
                    userSub.BillingCycle = billingCycle;
                }

                userSub.Status = "active";
                userSub.ExpiresAt = verification.ExpiresAt ?? userSub.ExpiresAt;
                userSub.PaymentRef = purchaseToken;
                ApplyCancellationState(userSub, verification);
                userSub.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (notificationType == 3) // CANCELED
        {
            userSub.CancelledAt = DateTime.UtcNow;
            userSub.UpdatedAt = DateTime.UtcNow;
        }
        else if (notificationType is 5 or 6 or 13) // EXPIRED, ON_HOLD, EXPIRED (v2)
        {
            userSub.Status = "expired";
            userSub.UpdatedAt = DateTime.UtcNow;
        }
        else if (notificationType == 10) // PAUSED
        {
            userSub.Status = "paused";
            userSub.UpdatedAt = DateTime.UtcNow;
        }
        else if (notificationType == 12) // REVOKED (Refunded)
        {
            userSub.Status = "refunded";
            userSub.CancelledAt = DateTime.UtcNow;
            userSub.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Updated Google subscription for user {UserId}, notification {Type}",
            userSub.UserId,
            notificationType);
    }

    private static string ResolveBillingCycle(string requestedCycle, string? basePlanId)
    {
        if (!string.IsNullOrWhiteSpace(basePlanId) &&
            basePlanId.Contains("yearly", StringComparison.OrdinalIgnoreCase))
        {
            return "yearly";
        }

        return "monthly";
    }

    private async Task<SubscriptionPlan?> ResolveGooglePlanAsync(
        string productId,
        string billingCycle,
        string? basePlanId)
    {
        if (!string.IsNullOrWhiteSpace(basePlanId))
        {
            var byBasePlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.GoogleProductId == basePlanId && p.DeletedAt == null);
            if (byBasePlan != null)
            {
                return byBasePlan;
            }
        }

        var byProduct = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.GoogleProductId == productId && p.DeletedAt == null);
        if (byProduct != null)
        {
            return byProduct;
        }

        if (string.Equals(productId, GoogleSubscriptionProductId, StringComparison.OrdinalIgnoreCase))
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == "premium_monthly" && p.DeletedAt == null && p.IsActive);
        }

        return null;
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
