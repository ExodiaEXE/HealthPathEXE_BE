using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Services;

public class IapVerificationService : IIapVerificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IapVerificationService> _logger;
    private readonly HttpClient _httpClient;

    public IapVerificationService(
        IConfiguration configuration,
        ILogger<IapVerificationService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IapVerificationResult> VerifyAndroidPurchaseAsync(string productId, string purchaseToken)
    {
        _logger.LogInformation(
            "Verifying Android purchase for product {ProductId}",
            productId);

        if (string.IsNullOrWhiteSpace(purchaseToken))
        {
            return InvalidAndroid("Thiếu purchase token từ Google Play.");
        }

        bool isMockMode = _configuration.GetValue("IAP:MockMode", true);
        var serviceAccountKey = _configuration["IAP:Google:ServiceAccountKey"];

        if (isMockMode || string.IsNullOrWhiteSpace(serviceAccountKey))
        {
            return VerifyAndroidMock(purchaseToken);
        }

        if (purchaseToken.Equals("mock_google_play_dev", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidAndroid(
                "Token mock không được chấp nhận khi IAP:MockMode=false.");
        }

        try
        {
            var packageName = string.IsNullOrWhiteSpace(_configuration["IAP:Google:PackageName"])
                ? "com.exodiateam.healthpath"
                : _configuration["IAP:Google:PackageName"]!;

            var publisher = CreateAndroidPublisherService(serviceAccountKey);
            var subscription = await publisher.Purchases.Subscriptionsv2
                .Get(packageName, purchaseToken)
                .ExecuteAsync();

            var lineItem = subscription.LineItems?
                .FirstOrDefault(li =>
                    string.Equals(li.ProductId, productId, StringComparison.OrdinalIgnoreCase))
                ?? subscription.LineItems?.FirstOrDefault();

            if (lineItem == null)
            {
                return InvalidAndroid(
                    $"Google Play không trả về line item cho sản phẩm {productId}.");
            }

            if (!string.Equals(lineItem.ProductId, productId, StringComparison.OrdinalIgnoreCase))
            {
                return InvalidAndroid(
                    $"Product ID không khớp: yêu cầu {productId}, Google trả về {lineItem.ProductId}.");
            }

            var utcNow = DateTime.UtcNow;
            if (!IsGoogleSubscriptionEntitled(subscription, utcNow))
            {
                var state = subscription.SubscriptionState ?? "unknown";
                return InvalidAndroid(
                    $"Gói Google Play chưa active (state={state}).");
            }

            var expiresAt = lineItem.ExpiryTimeDateTimeOffset?.UtcDateTime;
            if (expiresAt == null || expiresAt <= utcNow)
            {
                return InvalidAndroid("Gói Google Play đã hết hạn.");
            }

            if (string.Equals(
                    subscription.AcknowledgementState,
                    "ACKNOWLEDGEMENT_STATE_PENDING",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Acknowledging Google subscription {ProductId} for package {Package}",
                    productId,
                    packageName);

                await publisher.Purchases.Subscriptions.Acknowledge(
                        new SubscriptionPurchasesAcknowledgeRequest(),
                        packageName,
                        productId,
                        purchaseToken)
                    .ExecuteAsync();
            }

            var purchasedAt = subscription.StartTimeDateTimeOffset?.UtcDateTime ?? utcNow;
            var orderId = subscription.LatestOrderId ?? purchaseToken;

            _logger.LogInformation(
                "Google Play verification OK: order={OrderId}, expires={ExpiresAt}",
                orderId,
                expiresAt);

            return new IapVerificationResult
            {
                IsValid = true,
                PlatformTransactionId = orderId,
                OriginalTransactionId = purchaseToken,
                PurchasedAt = purchasedAt,
                ExpiresAt = expiresAt,
                Amount = 0,
                Currency = "VND",
            };
        }
        catch (Google.GoogleApiException ex)
        {
            _logger.LogError(
                ex,
                "Google Play Publisher API error ({StatusCode}): {Message}",
                ex.HttpStatusCode,
                ex.Message);

            return InvalidAndroid(
                $"Google Play API ({(int)ex.HttpStatusCode}): {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Android IAP with Google Play APIs");
            return InvalidAndroid($"Google Play API Error: {ex.Message}");
        }
    }

    private IapVerificationResult VerifyAndroidMock(string purchaseToken)
    {
        _logger.LogWarning("Android IAP Verification is running in MOCK mode.");

        if (purchaseToken.Equals("fail_token", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidAndroid("Mock Verification: Invalid purchase token.");
        }

        return new IapVerificationResult
        {
            IsValid = true,
            PlatformTransactionId = $"gplay_{Guid.NewGuid():N}",
            OriginalTransactionId = purchaseToken,
            PurchasedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Amount = 0,
            Currency = "VND",
        };
    }

    private static bool IsGoogleSubscriptionEntitled(
        SubscriptionPurchaseV2 subscription,
        DateTime utcNow)
    {
        var expiry = subscription.LineItems?
            .Select(li => li.ExpiryTimeDateTimeOffset?.UtcDateTime)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .DefaultIfEmpty()
            .Max();

        if (expiry > utcNow)
        {
            return true;
        }

        var state = subscription.SubscriptionState ?? string.Empty;
        return state is "SUBSCRIPTION_STATE_ACTIVE"
            or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD";
    }

    private AndroidPublisherService CreateAndroidPublisherService(string serviceAccountKey)
    {
        GoogleCredential credential;
        var trimmed = serviceAccountKey.Trim();

        if (trimmed.StartsWith('{'))
        {
            credential = GoogleCredential.FromJson(trimmed);
        }
        else
        {
            var path = Path.IsPathRooted(trimmed)
                ? trimmed
                : Path.Combine(Directory.GetCurrentDirectory(), trimmed);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Google Play service account key not found: {path}");
            }

            credential = GoogleCredential.FromFile(path);
        }

        credential = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

        return new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "HealthPath API",
        });
    }

    private static IapVerificationResult InvalidAndroid(string message) =>
        new()
        {
            IsValid = false,
            ErrorMessage = message,
        };

    public async Task<IapVerificationResult> VerifyIosPurchaseAsync(string productId, string receiptData)
    {
        _logger.LogInformation("Verifying iOS Purchase for Product {ProductId}", productId);

        bool isMockMode = _configuration.GetValue("IAP:MockMode", true);
        if (isMockMode || string.IsNullOrWhiteSpace(_configuration["IAP:Apple:SharedSecret"]))
        {
            _logger.LogWarning("iOS IAP Verification is running in MOCK mode.");
            if (receiptData.Equals("fail_token", StringComparison.OrdinalIgnoreCase))
            {
                return new IapVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Mock Verification: Invalid App Store receipt."
                };
            }

            return new IapVerificationResult
            {
                IsValid = true,
                PlatformTransactionId = $"appstore_{Guid.NewGuid():N}",
                OriginalTransactionId = $"appstore_orig_{Guid.NewGuid():N}",
                PurchasedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Amount = 0,
                Currency = "VND"
            };
        }

        // Real Apple App Store Receipt Verification (Sandbox fallback to Production)
        try
        {
            string sharedSecret = _configuration["IAP:Apple:SharedSecret"] ?? "";
            var payload = new
            {
                receipt_data = receiptData,
                password = sharedSecret,
                exclude_old_transactions = true
            };

            // First, call App Store Production Endpoint
            string url = "https://buy.itunes.apple.com/verifyReceipt";
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var resultJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            int status = root.GetProperty("status").GetInt32();

            // Status 21007 indicates Sandbox receipt sent to Production server. Fallback to Sandbox.
            if (status == 21007)
            {
                _logger.LogInformation("Production verifyReceipt returned 21007. Trying Sandbox endpoint...");
                url = "https://sandbox.itunes.apple.com/verifyReceipt";
                response = await _httpClient.PostAsJsonAsync(url, payload);
                resultJson = await response.Content.ReadAsStringAsync();

                using var sandboxDoc = JsonDocument.Parse(resultJson);
                root = sandboxDoc.RootElement;
                status = root.GetProperty("status").GetInt32();
            }

            if (status == 0)
            {
                // Successful verification
                _logger.LogInformation("App Store Verification Succeeded.");

                // Parse transaction details from receipts
                // Typically we inspect "latest_receipt_info" array
                if (root.TryGetProperty("latest_receipt_info", out var latestReceiptInfo) && latestReceiptInfo.GetArrayLength() > 0)
                {
                    var lastReceipt = latestReceiptInfo[latestReceiptInfo.GetArrayLength() - 1];
                    string transactionId = lastReceipt.GetProperty("transaction_id").GetString()!;
                    string originalTransactionId = lastReceipt.GetProperty("original_transaction_id").GetString()!;

                    // Parse expiration date milliseconds
                    long expiresDateMs = long.Parse(lastReceipt.GetProperty("expires_date_ms").GetString()!);
                    long purchaseDateMs = long.Parse(lastReceipt.GetProperty("purchase_date_ms").GetString()!);

                    DateTime expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresDateMs).UtcDateTime;
                    DateTime purchasedAt = DateTimeOffset.FromUnixTimeMilliseconds(purchaseDateMs).UtcDateTime;

                    return new IapVerificationResult
                    {
                        IsValid = true,
                        PlatformTransactionId = transactionId,
                        OriginalTransactionId = originalTransactionId,
                        PurchasedAt = purchasedAt,
                        ExpiresAt = expiresAt,
                        Amount = 0,
                        Currency = "VND"
                    };
                }

                return new IapVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "App Store receipt was valid but contained no latest_receipt_info."
                };
            }

            _logger.LogError("App Store Verification Failed with status: {Status}", status);
            return new IapVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"App Store verification failed with status: {status}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify iOS IAP with App Store");
            return new IapVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"App Store API Error: {ex.Message}"
            };
        }
    }
}
