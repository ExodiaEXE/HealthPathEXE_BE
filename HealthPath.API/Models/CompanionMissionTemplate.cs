using System;

namespace HealthPath.API.Models;

public class CompanionMissionTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "daily";
    public int TargetCount { get; set; } = 1;
    public int RewardCoins { get; set; }
    public int RewardXp { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
