using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthPath.API.Services.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();

    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? Context.User?.FindFirst("sub")?.Value; // fallback to "sub" claim
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections[userId] = Context.ConnectionId;
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections.TryRemove(userId, out _);
        }
        return base.OnDisconnectedAsync(exception);
    }

    public static string? GetConnectionId(string userId)
    {
        UserConnections.TryGetValue(userId, out var connectionId);
        return connectionId;
    }
}
