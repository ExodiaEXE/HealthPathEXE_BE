using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
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

    /// <summary>
    /// Nạp routine mẫu từ Document/seed_routines.json khi bảng routines còn trống.
    /// </summary>
    public static async Task SeedDefaultRoutinesAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<HealthpathDbContext>>();

        try
        {
            var context = services.GetRequiredService<HealthpathDbContext>();

            if (await context.Routines.AnyAsync())
            {
                logger.LogInformation("Bảng routines đã có dữ liệu — bỏ qua seed routine mẫu.");
                return;
            }

            var seedPath = ResolveSeedRoutinesPath();
            if (seedPath == null)
            {
                logger.LogWarning("Không tìm thấy seed_routines.json — bỏ qua seed routine mẫu.");
                return;
            }

            var json = await File.ReadAllTextAsync(seedPath);
            var payload = JsonSerializer.Deserialize<RoutineSeedFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.Routines == null || payload.Routines.Count == 0)
            {
                logger.LogWarning("seed_routines.json không có mục routines — bỏ qua.");
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var item in payload.Routines)
            {
                context.Routines.Add(new Routine
                {
                    Id = Guid.NewGuid(),
                    Title = item.Title,
                    Description = item.Description,
                    Category = item.Category,
                    Difficulty = item.Difficulty,
                    DurationMinutes = item.DurationMinutes,
                    IsPremium = item.IsPremium,
                    ThumbnailUrl = item.ThumbnailUrl,
                    IsSystem = true,
                    CreatedBy = null,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Đã seed {Count} routine mẫu từ {Path}.", payload.Routines.Count, seedPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi seed routine mẫu.");
        }
    }

    private static string? ResolveSeedRoutinesPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Document", "seed_routines.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Document", "seed_routines.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Document", "seed_routines.json"))
        };

        foreach (var path in candidates)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(full)) return full;
        }

        return null;
    }

    private sealed class RoutineSeedFile
    {
        public List<CreateRoutineDto> Routines { get; set; } = new();
    }
}
