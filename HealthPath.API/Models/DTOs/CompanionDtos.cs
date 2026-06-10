using System;
using System.Collections.Generic;

namespace HealthPath.API.Models.DTOs;

public class CompanionStateDto
{
    public int Level { get; set; }
    public int Xp { get; set; }
    public int XpForNextLevel { get; set; }
    public int Coins { get; set; }
    public int Hunger { get; set; }
    public int Happiness { get; set; }
    public int Energy { get; set; }
    public string RoomTheme { get; set; } = "cozy";
    public List<string> EquippedItemSkus { get; set; } = new();
    public bool CanFeed { get; set; }
    public bool CanPet { get; set; }
    public string? FeedBlockedReason { get; set; }
    public string? PetBlockedReason { get; set; }
    public int FeedCooldownSeconds { get; set; }
    public int PetCooldownSeconds { get; set; }
    public string MascotMood { get; set; } = "idle";
}

public class CompanionActionResultDto
{
    public CompanionStateDto State { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public int CoinsEarned { get; set; }
    public int XpEarned { get; set; }
}

public class CompanionCatalogItemDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Price { get; set; }
    public string IconEmoji { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public bool IsOwned { get; set; }
    public bool IsEquipped { get; set; }
}

public class CompanionMissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TargetCount { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public int RewardCoins { get; set; }
    public int RewardXp { get; set; }
}

public class CompanionMissionsResponseDto
{
    public string Category { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public List<CompanionMissionDto> Missions { get; set; } = new();
}

public class EquipCompanionItemDto
{
    public string Sku { get; set; } = string.Empty;
}

public class SetRoomThemeDto
{
    public string Theme { get; set; } = "cozy";
}

public class PurchaseCompanionItemDto
{
    public string Sku { get; set; } = string.Empty;
}

public class CompanionAssetsDto
{
    public string Version { get; set; } = "1";
    public bool Enable3D { get; set; } = true;
    public string MascotGlbUrl { get; set; } = string.Empty;
    public Dictionary<string, string> RoomSceneUrls { get; set; } = new();
    public Dictionary<string, string> MascotAnimations { get; set; } = new();
}
