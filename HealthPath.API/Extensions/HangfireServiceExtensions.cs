using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using HealthPath.API.BackgroundJobs;

namespace HealthPath.API.Extensions;

public static class HangfireServiceExtensions
{
    /// <summary>
    /// Linux/Docker dùng IANA "Asia/Ho_Chi_Minh"; Windows dùng "SE Asia Standard Time".
    /// </summary>
    private static TimeZoneInfo VietnamTimeZone
    {
        get
        {
            foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }

    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Đăng ký Hangfire Services sử dụng Database PostgreSQL làm Storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => 
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        // 2. Kích hoạt Background Job Server xử lý tác vụ
        services.AddHangfireServer();

        return services;
    }

    public static IApplicationBuilder UseHangfireJobs(this IApplicationBuilder app)
    {
        // 1. Kích hoạt Giao diện Dashboard tại /hangfire
        app.UseHangfireDashboard("/hangfire");

        // 2. Thiết lập Lịch trình cho Job Định Kỳ (Recurring Jobs)
        // Job lặp thói quen: Tự động chạy hàng ngày lúc 00:00 (Nửa đêm)
        RecurringJob.AddOrUpdate<IRecurringRoutineJob>(
            "recurring-routines",
            job => job.ExecuteAsync(),
            "0 0 * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );

        // Job quét bài tập lỡ: Tự động chạy hàng ngày lúc 23:50 đêm
        RecurringJob.AddOrUpdate<IMissDetectionJob>(
            "miss-detection",
            job => job.ExecuteAsync(),
            "50 23 * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );

        return app;
    }
}
