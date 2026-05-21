using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;

namespace HealthPath.API.Services.Channels;

public class PushChannel : INotificationChannel
{
    private readonly ILogger<PushChannel> _logger;

    public PushChannel(ILogger<PushChannel> logger)
    {
        _logger = logger;
    }

    public string Name => "push";

    public async Task SendAsync(HealthPath.API.Models.Notification notification, User user)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Firebase App is not initialized. Skipping Push Notification.");
            return;
        }

        // Retrieve active device tokens for the user
        var activeTokens = user.DeviceTokens?
            .Where(t => t.IsActive)
            .Select(t => t.Token)
            .ToList() ?? new List<string>();

        if (!activeTokens.Any())
        {
            _logger.LogInformation("No active device tokens found for User {UserId}. Skipping Push Notification.", user.Id);
            return;
        }

        try
        {
            var message = new MulticastMessage
            {
                Tokens = activeTokens,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = notification.Title,
                    Body = notification.Body
                },
                Data = new Dictionary<string, string>
                {
                    { "id", notification.Id.ToString() },
                    { "type", notification.Type },
                    { "data", notification.Data ?? "{}" }
                }
            };

            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
            _logger.LogInformation("Successfully sent {SuccessCount} push notifications. Failed: {FailureCount}", 
                response.SuccessCount, response.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notifications via Firebase FCM");
        }
    }
}
