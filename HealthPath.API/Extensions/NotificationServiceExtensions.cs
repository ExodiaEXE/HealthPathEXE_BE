using Microsoft.Extensions.DependencyInjection;
using HealthPath.API.Services;
using HealthPath.API.Services.Channels;

namespace HealthPath.API.Extensions;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        // 1. Đăng ký Real-time SignalR
        services.AddSignalR();

        // 2. Đăng ký các kênh truyền tải thông báo (Notification Channels)
        services.AddScoped<INotificationChannel, InAppChannel>();
        services.AddScoped<INotificationChannel, PushChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();

        // 3. Đăng ký Dịch vụ thông báo cốt lõi (Core Notification Service)
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
