using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Extensions;

public static class SubscriptionPlanSeederExtension
{
    private const string MonthlyGoogleProductId = "healthpath_premium_monthly";
    private const string YearlyGoogleProductId = "healthpath_premium_yearly";

    public static async Task SeedDefaultSubscriptionPlansAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<HealthpathDbContext>>();

        try
        {
            var context = services.GetRequiredService<HealthpathDbContext>();
            var now = DateTime.UtcNow;

            if (!await context.SubscriptionPlans.AnyAsync(p => p.Code == "premium_monthly" && p.DeletedAt == null))
            {
                context.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "HealthPath Cao cấp — Tháng",
                    Code = "premium_monthly",
                    Description = "Gói đăng ký hàng tháng qua Google Play",
                    PriceMonthly = 59000,
                    PriceYearly = 590000,
                    Currency = "VND",
                    Features = """["Không quảng cáo","Toàn bộ audio thư giãn","Hỗ trợ ưu tiên"]""",
                    IsActive = true,
                    GoogleProductId = MonthlyGoogleProductId,
                    AppleProductId = "healthpath_premium_monthly_ios",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                logger.LogInformation("Seeded subscription plan premium_monthly ({ProductId}).", MonthlyGoogleProductId);
            }

            if (!await context.SubscriptionPlans.AnyAsync(p => p.Code == "premium_yearly" && p.DeletedAt == null))
            {
                context.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "HealthPath Cao cấp — Năm",
                    Code = "premium_yearly",
                    Description = "Gói đăng ký hàng năm qua Google Play",
                    PriceMonthly = 59000,
                    PriceYearly = 590000,
                    Currency = "VND",
                    Features = """["Không quảng cáo","Toàn bộ audio thư giãn","Hỗ trợ ưu tiên"]""",
                    IsActive = true,
                    GoogleProductId = YearlyGoogleProductId,
                    AppleProductId = "healthpath_premium_yearly_ios",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                logger.LogInformation("Seeded subscription plan premium_yearly ({ProductId}).", YearlyGoogleProductId);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi seed gói subscription mặc định.");
        }
    }
}
