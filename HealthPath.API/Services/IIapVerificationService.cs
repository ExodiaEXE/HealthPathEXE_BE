using System.Threading.Tasks;

namespace HealthPath.API.Services;

public interface IIapVerificationService
{
    Task<IapVerificationResult> VerifyAndroidPurchaseAsync(string productId, string purchaseToken);
    Task<IapVerificationResult> VerifyIosPurchaseAsync(string productId, string receiptData);
}
