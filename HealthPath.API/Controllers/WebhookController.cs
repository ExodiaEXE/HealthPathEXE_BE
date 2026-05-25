using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(ISubscriptionService subscriptionService, ILogger<WebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost("google-play")]
    public async Task<IActionResult> GooglePlayNotification([FromBody] JsonElement payload)
    {
        _logger.LogInformation("Received Google Play Webhook RTDN");
        var response = await _subscriptionService.ProcessServerNotificationAsync("GooglePlay", payload);
        if (!response.Success)
        {
            _logger.LogError("Failed to process Google Play Webhook: {Message}", response.Message);
            // We return Ok(200) to Google to acknowledge receipt but log the error so Google doesn't retry indefinitely
        }
        return Ok(new { success = response.Success });
    }

    [HttpPost("app-store")]
    public async Task<IActionResult> AppStoreNotification([FromBody] JsonElement payload)
    {
        _logger.LogInformation("Received Apple App Store Server Webhook Notification");
        var response = await _subscriptionService.ProcessServerNotificationAsync("AppStore", payload);
        if (!response.Success)
        {
            _logger.LogError("Failed to process App Store Webhook: {Message}", response.Message);
            // We return Ok(200) to Apple to acknowledge receipt but log the error
        }
        return Ok(new { success = response.Success });
    }
}
