using Microsoft.Extensions.DependencyInjection;
using HealthPath.API.Services;

namespace HealthPath.API.Extensions;

public static class AudioServiceExtensions
{
    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        services.AddScoped<IAudioTrackService, AudioTrackService>();
        return services;
    }
}
