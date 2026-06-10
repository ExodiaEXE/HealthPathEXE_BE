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

    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        string postgresConnection)
    {
        // 1. Đăng ký Hangfire Services sử dụng Database PostgreSQL làm Storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                bootstrap => bootstrap.UseNpgsqlConnection(postgresConnection),
                new PostgreSqlStorageOptions
                {
                    DistributedLockTimeout = TimeSpan.FromMinutes(2),
                    QueuePollInterval = TimeSpan.FromSeconds(60),
                }));

        // 2. Ít worker hơn → ít giữ connection DB (dùng chung pool với EF Core)
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(60);
        });

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

        // Nhắc check-in: 8:00 sáng và 20:00 tối (VN)
        RecurringJob.AddOrUpdate<IDailyCheckinReminderJob>(
            "daily-checkin-morning",
            job => job.ExecuteAsync(),
            "0 8 * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );
        RecurringJob.AddOrUpdate<IDailyCheckinReminderJob>(
            "daily-checkin-evening",
            job => job.ExecuteAsync(),
            "0 20 * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );

        // Nhắc thói quen theo giờ lên lịch — mỗi 30 phút
        RecurringJob.AddOrUpdate<IRoutineReminderJob>(
            "routine-reminder",
            job => job.ExecuteAsync(),
            "*/30 * * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );

        RecurringJob.AddOrUpdate<ICompanionDecayJob>(
            "companion-decay",
            job => job.ExecuteAsync(),
            "0 * * * *",
            new RecurringJobOptions { TimeZone = VietnamTimeZone }
        );

        return app;
    }
}
