using System;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Extensions;

public static class DbSeederExtension
{
    public static async Task SeedDefaultAdminAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<HealthpathDbContext>>();

        try
        {
            var context = services.GetRequiredService<HealthpathDbContext>();

            // Lấy thông tin từ biến môi trường (hoặc fallback về mặc định nếu không cấu hình)
            var adminUsername = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_USERNAME") ?? "admin";
            var adminPassword = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? "admin@123";
            var adminEmail = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_EMAIL") ?? "admin@healthpath.vn";

            var adminExists = await context.Admins.AnyAsync(a => a.Username == adminUsername);

            if (!adminExists)
            {
                logger.LogInformation("Đang khởi tạo tài khoản Admin mặc định...");

                var admin = new Admin
                {
                    Id = Guid.NewGuid(),
                    Username = adminUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    FullName = "Super Administrator",
                    Email = adminEmail,
                    Role = "SuperAdmin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Admins.Add(admin);
                await context.SaveChangesAsync();

                logger.LogInformation("Khởi tạo Admin thành công với Username: {Username}", adminUsername);
            }
            else
            {
                logger.LogInformation("Tài khoản Admin đã tồn tại. Bỏ qua bước seed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo tài khoản Admin mặc định.");
        }
    }
}
