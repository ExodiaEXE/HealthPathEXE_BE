using Microsoft.Extensions.DependencyInjection;
using HealthPath.API.Services;

namespace HealthPath.API.Extensions;

public static class SubscriptionServiceExtensions
{
    public static IServiceCollection AddSubscriptionServices(this IServiceCollection services)
    {
        services.AddHttpClient<IIapVerificationService, IapVerificationService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        return services;
    }
}
