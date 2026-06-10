using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using HealthPath.API.Services;
using HealthPath.API.Services.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthPath.API.Extensions;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        InitializeFirebaseIfConfigured(configuration);

        services.AddSignalR();

        services.AddScoped<INotificationChannel, InAppChannel>();
        services.AddScoped<INotificationChannel, PushChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();

        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());

        return services;
    }

    static void InitializeFirebaseIfConfigured(IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance != null) return;

        var credentialPath = configuration["Firebase:CredentialPath"];
        if (string.IsNullOrWhiteSpace(credentialPath)) return;

        var fullPath = Path.IsPathRooted(credentialPath)
            ? credentialPath
            : Path.Combine(Directory.GetCurrentDirectory(), credentialPath);

        if (!File.Exists(fullPath)) return;

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(fullPath),
        });
    }
}
