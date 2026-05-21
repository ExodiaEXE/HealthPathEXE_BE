using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HealthPath.API.Options;
using HealthPath.API.Services;

namespace HealthPath.API.Extensions;

public static class FileStorageServiceExtensions
{
    public static IServiceCollection AddFileStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Cấu hình Options giải mã Cloudflare R2
        services.Configure<CloudflareR2Options>(configuration.GetSection("CloudflareR2"));

        // 2. Đăng ký dịch vụ lưu trữ File (Cloudflare R2 + Local fallback)
        services.AddScoped<IFileStorageService, CloudflareR2Service>();

        return services;
    }
}
