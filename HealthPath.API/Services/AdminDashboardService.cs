using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly HealthpathDbContext _context;

    public AdminDashboardService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DashboardDto>> GetDashboardStatsAsync()
    {
        // 1. User counters
        int totalUsers = await _context.Users.CountAsync(u => u.DeletedAt == null);
        
        int totalPremiumUsers = await _context.UserSubscriptions
            .CountAsync(s => s.Status == "active" && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow) && s.DeletedAt == null);

        double conversionRate = totalUsers > 0 
            ? Math.Round(((double)totalPremiumUsers / totalUsers) * 100, 2) 
            : 0;

        // 2. Total Revenue
        decimal totalRevenue = await _context.Transactions
            .Where(t => t.Status == "Success")
            .SumAsync(t => t.Amount);

        // 3. Platform Breakdown
        var platformData = await _context.Transactions
            .Where(t => t.Status == "Success")
            .GroupBy(t => t.Platform)
            .Select(g => new PlatformRevenueItem
            {
                Platform = g.Key,
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync();

        // 4. Monthly Chart Data (Last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var startOfPeriod = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var recentTransactionsForChart = await _context.Transactions
            .Where(t => t.Status == "Success" && t.PurchasedAt >= startOfPeriod)
            .ToListAsync();

        var monthlyChart = new List<MonthlyRevenueItem>();
        for (int i = 0; i < 6; i++)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-5 + i);
            string monthStr = targetMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);

            var txsInMonth = recentTransactionsForChart
                .Where(t => t.PurchasedAt.Year == targetMonth.Year && t.PurchasedAt.Month == targetMonth.Month)
                .ToList();

            monthlyChart.Add(new MonthlyRevenueItem
            {
                Month = monthStr,
                Revenue = txsInMonth.Sum(t => t.Amount),
                TransactionCount = txsInMonth.Count
            });
        }

        // 5. Recent Transactions (Top 5)
        var recentTxs = await _context.Transactions
            .Include(t => t.Plan)
            .OrderByDescending(t => t.PurchasedAt)
            .Take(5)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                PlanId = t.PlanId,
                PlanName = t.Plan.Name,
                Platform = t.Platform,
                PlatformTransactionId = t.PlatformTransactionId,
                OriginalTransactionId = t.OriginalTransactionId,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                PurchasedAt = t.PurchasedAt,
                ExpiresAt = t.ExpiresAt,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var dashboardDto = new DashboardDto
        {
            TotalUsers = totalUsers,
            TotalPremiumUsers = totalPremiumUsers,
            ConversionRate = conversionRate,
            TotalRevenue = totalRevenue,
            PlatformBreakdown = platformData,
            MonthlyRevenueChart = monthlyChart,
            RecentTransactions = recentTxs
        };

        return ApiResponse<DashboardDto>.Ok(dashboardDto, "Lấy số liệu thống kê Dashboard thành công.");
    }
}
