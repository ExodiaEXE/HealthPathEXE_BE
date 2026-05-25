using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Extensions;

public static class DatabaseMigrationExtension
{
    private const string ProductVersion = "10.0.8";

    private static readonly string[] MigrationIds =
    [
        "20260519141727_AddUserStatsAndRecurringTemplate",
        "20260519141935_UpdateDifficultyDefaultToEnglish",
        "20260520030635_FixMissingColumnUserRoutine",
        "20260520142745_RemoveScoreAndAiInsights",
        "20260521035840_AddDeviceTokensTable",
        "20260521135505_AddAudioCategoriesAndFavorites",
        "20260525051437_AddSubscriptionIapAndTransactions",
        "20260525052128_AddAdminTableAndSeed"
    ];

    public static async Task ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthpathDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HealthpathDbContext>>();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        if (applied.Count == 0 && await TableExistsAsync(context, "users"))
        {
            var lastBaselineIndex = await TableExistsAsync(context, "transactions")
                ? Array.IndexOf(MigrationIds, "20260525051437_AddSubscriptionIapAndTransactions")
                : Array.IndexOf(MigrationIds, "20260521135505_AddAudioCategoriesAndFavorites");

            logger.LogWarning(
                "Database đã có schema nhưng chưa ghi migration history. Đang baseline {Count} migration(s)...",
                lastBaselineIndex + 1);

            for (var i = 0; i <= lastBaselineIndex; i++)
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ({0}, {1})
                    ON CONFLICT ("MigrationId") DO NOTHING
                    """,
                    MigrationIds[i],
                    ProductVersion);
            }
        }

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Đang áp dụng {Count} migration(s) pending: {Migrations}",
                pending.Count, string.Join(", ", pending));
            await context.Database.MigrateAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(HealthpathDbContext context, string tableName)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = @tableName
                )
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return result is true or 1 or (long)1;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
