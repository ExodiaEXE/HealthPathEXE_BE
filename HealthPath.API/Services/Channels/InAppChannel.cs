using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services.Hubs;

namespace HealthPath.API.Services.Channels;

public class InAppChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public InAppChannel(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public string Name => "in_app";

    public async Task SendAsync(Notification notification, User user)
    {
        var connectionId = NotificationHub.GetConnectionId(user.Id.ToString());
        if (string.IsNullOrEmpty(connectionId))
        {
            // User is not connected, but that's fine for in-app as they'll pull it later
            return;
        }

        var dto = new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            Data = notification.Data,
            Channel = notification.Channel,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            SentAt = notification.SentAt,
            CreatedAt = notification.CreatedAt
        };

        // Notify client real-time
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", dto);
    }
}
