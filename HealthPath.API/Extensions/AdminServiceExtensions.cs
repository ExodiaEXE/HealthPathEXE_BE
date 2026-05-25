using Microsoft.Extensions.DependencyInjection;
using HealthPath.API.Services;

namespace HealthPath.API.Extensions;

public static class AdminServiceExtensions
{
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminSubscriptionService, AdminSubscriptionService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddMemoryCache(); // Đăng ký IMemoryCache để lưu Permission
        return services;
    }
}
