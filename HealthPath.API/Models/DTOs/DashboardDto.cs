using System;
using System.Collections.Generic;

namespace HealthPath.API.Models.DTOs;

public class DashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalPremiumUsers { get; set; }
    public double ConversionRate { get; set; } // Percentage of premium users
    public decimal TotalRevenue { get; set; }
    
    public List<PlatformRevenueItem> PlatformBreakdown { get; set; } = new();
    
    public List<MonthlyRevenueItem> MonthlyRevenueChart { get; set; } = new();
    
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}

public class PlatformRevenueItem
{
    public string Platform { get; set; } = null!;
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class MonthlyRevenueItem
{
    public string Month { get; set; } = null!; // "2026-05"
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}
