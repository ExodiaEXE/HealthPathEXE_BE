using Npgsql;

namespace HealthPath.API.Extensions;

/// <summary>
/// EF Core → Supabase session/transaction pooler.
/// Hangfire → session pooler (:5432) — cần advisory lock; pool riêng với EF.
/// Supabase session mode ~15 conn/project — tổng EF + Hangfire client pool nên ≤ 12.
/// </summary>
public static class DatabaseConnectionExtensions
{
    public const int EfMaxPoolSize = 5;
    /// <summary>Hangfire storage + worker + dashboard cần ≥ 3 conn đồng thời.</summary>
    public const int HangfireMaxPoolSize = 4;
    public const int ConnectionTimeoutSeconds = 45;

    public static string NormalizePostgresConnection(
        string? connectionString,
        int maxPoolSize = EfMaxPoolSize,
        bool? forTransactionPooler = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection chưa được cấu hình (.env hoặc appsettings).");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var currentMax = builder.MaxPoolSize <= 0 ? maxPoolSize : builder.MaxPoolSize;
        var isTransactionPooler = forTransactionPooler ?? builder.Port == 6543;

        builder.MaxPoolSize = Math.Min(currentMax, maxPoolSize);
        builder.MinPoolSize = 0;
        builder.Pooling = true;
        builder.Timeout = ConnectionTimeoutSeconds;
        builder.CommandTimeout = 30;
        builder.KeepAlive = 30;
        builder.TcpKeepAlive = true;
        builder.ConnectionIdleLifetime = 30;
        builder.ConnectionPruningInterval = 15;

        if (isTransactionPooler)
        {
            builder.MaxAutoPrepare = 0;
        }

        return builder.ConnectionString;
    }

    public static string ResolveHangfireConnection(IConfiguration configuration)
    {
        var dedicated = configuration.GetConnectionString("HangfireConnection");
        if (!string.IsNullOrWhiteSpace(dedicated))
        {
            return NormalizePostgresConnection(
                dedicated,
                maxPoolSize: HangfireMaxPoolSize,
                forTransactionPooler: false);
        }

        var fallback = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(fallback))
        {
            throw new InvalidOperationException(
                "Cần ConnectionStrings:HangfireConnection hoặc DefaultConnection.");
        }

        var builder = new NpgsqlConnectionStringBuilder(fallback)
        {
            Port = 5432,
        };

        return NormalizePostgresConnection(
            builder.ConnectionString,
            maxPoolSize: HangfireMaxPoolSize,
            forTransactionPooler: false);
    }
}
