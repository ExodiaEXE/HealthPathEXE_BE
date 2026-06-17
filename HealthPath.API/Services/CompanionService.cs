using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthPath.API.Services;

public interface ICompanionService
{
    Task<ApiResponse<CompanionStateDto>> GetStateAsync(Guid userId);
    Task<ApiResponse<CompanionActionResultDto>> FeedAsync(Guid userId);
    Task<ApiResponse<CompanionActionResultDto>> PetAsync(Guid userId);
    Task<ApiResponse<CompanionMissionsResponseDto>> GetMissionsAsync(Guid userId, string category);
    Task<ApiResponse<List<CompanionCatalogItemDto>>> GetCatalogAsync(Guid userId, string? category);
    Task<ApiResponse<CompanionCatalogItemDto>> PurchaseAsync(Guid userId, PurchaseCompanionItemDto dto);
    Task<ApiResponse<CompanionStateDto>> EquipAsync(Guid userId, EquipCompanionItemDto dto);
    Task<ApiResponse<CompanionStateDto>> SetRoomThemeAsync(Guid userId, SetRoomThemeDto dto);
    Task ReportEventAsync(Guid userId, string eventCode, int increment = 1);
    CompanionAssetsDto GetAssets();
}

public class CompanionService : ICompanionService
{
    private const int FeedCost = 10;
    private const int FeedCooldownMinutes = 30;
    private const int PetCooldownMinutes = 5;
    private static readonly int[] LevelXpThresholds = [0, 100, 300, 600, 1000, 1500, 2100, 2800];

    private readonly HealthpathDbContext _db;
    private readonly ILogger<CompanionService> _logger;
    private readonly CompanionAssetsOptions _assets;

    public CompanionService(
        HealthpathDbContext db,
        ILogger<CompanionService> logger,
        IOptions<CompanionAssetsOptions> assets)
    {
        _db = db;
        _logger = logger;
        _assets = assets.Value;
    }

    public CompanionAssetsDto GetAssets()
    {
        var roomUrls = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(_assets.RoomCozyGlbUrl))
        {
            roomUrls["cozy"] = _assets.RoomCozyGlbUrl!;
            roomUrls["room_1"] = _assets.RoomCozyGlbUrl!;
        }
        if (!string.IsNullOrWhiteSpace(_assets.RoomModernGlbUrl))
        {
            roomUrls["modern"] = _assets.RoomModernGlbUrl!;
            roomUrls["room_2"] = _assets.RoomModernGlbUrl!;
        }
        if (!string.IsNullOrWhiteSpace(_assets.RoomNatureGlbUrl))
        {
            roomUrls["nature"] = _assets.RoomNatureGlbUrl!;
            roomUrls["room_3"] = _assets.RoomNatureGlbUrl!;
        }

        return new CompanionAssetsDto
        {
            Version = _assets.Version,
            Enable3D = _assets.Enable3D,
            MascotGlbUrl = _assets.MascotGlbUrl,
            RoomSceneUrls = roomUrls,
            MascotAnimations = new Dictionary<string, string>
            {
                ["idle"] = _assets.AnimationIdle,
                ["happy"] = _assets.AnimationHappy,
                ["eat"] = _assets.AnimationEat,
                ["sad"] = _assets.AnimationSad,
                ["sleepy"] = _assets.AnimationSleepy,
                ["wave"] = _assets.AnimationWave,
                ["hungry"] = _assets.AnimationHungry,
            },
        };
    }

    public async Task<ApiResponse<CompanionStateDto>> GetStateAsync(Guid userId)
    {
        await EnsureSeedDataAsync();
        var pet = await GetOrCreatePetAsync(userId);
        ApplyDecay(pet);
        await GrantDefaultInventoryAsync(userId);
        await ReportEventAsync(userId, "daily_login");
        await SyncHappinessMissionAsync(userId, pet.Happiness);
        pet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResponse<CompanionStateDto>.Ok(MapState(pet));
    }

    public async Task<ApiResponse<CompanionActionResultDto>> FeedAsync(Guid userId)
    {
        var pet = await GetOrCreatePetAsync(userId);
        ApplyDecay(pet);
        var (canFeed, reason) = CanFeed(pet);
        if (!canFeed)
        {
            return ApiResponse<CompanionActionResultDto>.Fail(reason!, ErrorCode.VALIDATION_ERROR);
        }

        pet.Coins -= FeedCost;
        pet.Hunger = Math.Min(100, pet.Hunger + 25);
        pet.Happiness = Math.Min(100, pet.Happiness + 5);
        pet.Energy = Math.Min(100, pet.Energy + 10);
        AddXp(pet, 5);
        pet.LastFeedAt = DateTime.UtcNow;
        pet.UpdatedAt = DateTime.UtcNow;
        await ReportEventAsync(userId, "feed_pet");
        await _db.SaveChangesAsync();

        return ApiResponse<CompanionActionResultDto>.Ok(new CompanionActionResultDto
        {
            State = MapState(pet),
            Message = "Đã cho ăn! Mèo Xanh no nê rồi 🐱",
            XpEarned = 5,
        });
    }

    public async Task<ApiResponse<CompanionActionResultDto>> PetAsync(Guid userId)
    {
        var pet = await GetOrCreatePetAsync(userId);
        ApplyDecay(pet);
        var (canPet, reason) = CanPet(pet);
        if (!canPet)
        {
            return ApiResponse<CompanionActionResultDto>.Fail(reason!, ErrorCode.VALIDATION_ERROR);
        }

        pet.Happiness = Math.Min(100, pet.Happiness + 15);
        pet.Energy = Math.Max(0, pet.Energy - 5);
        AddXp(pet, 3);
        pet.LastPetAt = DateTime.UtcNow;
        pet.UpdatedAt = DateTime.UtcNow;
        await ReportEventAsync(userId, "pet_interact", 1);
        await SyncHappinessMissionAsync(userId, pet.Happiness);
        await _db.SaveChangesAsync();

        return ApiResponse<CompanionActionResultDto>.Ok(new CompanionActionResultDto
        {
            State = MapState(pet),
            Message = "Mèo Xanh rất vui khi được vuốt ve 💚",
            XpEarned = 3,
        });
    }

    public async Task<ApiResponse<CompanionMissionsResponseDto>> GetMissionsAsync(Guid userId, string category)
    {
        await EnsureSeedDataAsync();
        var normalized = NormalizeCategory(category);
        var templates = await _db.CompanionMissionTemplates
            .Where(t => t.IsActive && t.Category == normalized)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        var pet = await GetOrCreatePetAsync(userId);
        await SyncHappinessMissionAsync(userId, pet.Happiness);

        var dateKey = normalized == "daily" ? TodayKey() : normalized == "weekly" ? WeekKey() : "lifetime";
        var progressList = await _db.CompanionMissionProgresses
            .Where(p => p.UserId == userId &&
                        templates.Select(t => t.Id).Contains(p.TemplateId) &&
                        p.DateKey == dateKey)
            .ToListAsync();

        var missions = templates.Select(t =>
        {
            var prog = progressList.FirstOrDefault(p => p.TemplateId == t.Id);
            var progress = prog?.Progress ?? 0;
            var completed = prog?.CompletedAt != null;

            if (t.Category == "once" && !completed)
            {
                progress = t.Code switch
                {
                    "once_level_5" => pet.Level >= 5 ? 1 : 0,
                    "once_coins_1000" => Math.Min(pet.Coins, t.TargetCount),
                    _ => progress,
                };
            }

            return new CompanionMissionDto
            {
                Id = t.Id,
                Code = t.Code,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category,
                TargetCount = t.TargetCount,
                Progress = Math.Min(progress, t.TargetCount),
                IsCompleted = completed,
                RewardCoins = t.RewardCoins,
                RewardXp = t.RewardXp,
            };
        }).ToList();

        return ApiResponse<CompanionMissionsResponseDto>.Ok(new CompanionMissionsResponseDto
        {
            Category = normalized,
            CompletedCount = missions.Count(m => m.IsCompleted),
            TotalCount = missions.Count,
            Missions = missions,
        });
    }

    public async Task<ApiResponse<List<CompanionCatalogItemDto>>> GetCatalogAsync(Guid userId, string? category)
    {
        await EnsureSeedDataAsync();
        await GrantDefaultInventoryAsync(userId);
        var query = _db.CompanionCatalogItems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        var items = await query.OrderBy(c => c.SortOrder).ToListAsync();
        var owned = await _db.CompanionInventories
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => i.CatalogItemId);

        var dtos = items.Select(c => new CompanionCatalogItemDto
        {
            Id = c.Id,
            Sku = c.Sku,
            Name = c.Name,
            Category = c.Category,
            Price = c.Price,
            IconEmoji = c.IconEmoji,
            PreviewUrl = c.PreviewUrl,
            IsOwned = owned.ContainsKey(c.Id),
            IsEquipped = owned.TryGetValue(c.Id, out var inv) && inv.IsEquipped,
        }).ToList();

        return ApiResponse<List<CompanionCatalogItemDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<CompanionCatalogItemDto>> PurchaseAsync(Guid userId, PurchaseCompanionItemDto dto)
    {
        await EnsureSeedDataAsync();
        var item = await _db.CompanionCatalogItems.FirstOrDefaultAsync(c => c.Sku == dto.Sku);
        if (item == null)
        {
            return ApiResponse<CompanionCatalogItemDto>.Fail("Không tìm thấy vật phẩm.", ErrorCode.VALIDATION_ERROR);
        }

        var already = await _db.CompanionInventories.AnyAsync(i =>
            i.UserId == userId && i.CatalogItemId == item.Id);
        if (already)
        {
            return ApiResponse<CompanionCatalogItemDto>.Fail("Bạn đã sở hữu vật phẩm này.", ErrorCode.VALIDATION_ERROR);
        }

        var pet = await GetOrCreatePetAsync(userId);
        if (pet.Coins < item.Price)
        {
            return ApiResponse<CompanionCatalogItemDto>.Fail("Không đủ xu.", ErrorCode.VALIDATION_ERROR);
        }

        pet.Coins -= item.Price;
        var inventory = new CompanionInventory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CatalogItemId = item.Id,
            IsEquipped = false,
            AcquiredAt = DateTime.UtcNow,
        };
        _db.CompanionInventories.Add(inventory);
        await _db.SaveChangesAsync();

        if (item.Category == "background")
        {
            var theme = RoomThemeForBackgroundSku(item.Sku);
            if (theme != null)
            {
                pet.RoomTheme = theme;
                await SyncBackgroundEquipAsync(userId, theme);
                pet.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        var isEquipped = await _db.CompanionInventories
            .AnyAsync(i => i.UserId == userId && i.CatalogItemId == item.Id && i.IsEquipped);

        return ApiResponse<CompanionCatalogItemDto>.Ok(new CompanionCatalogItemDto
        {
            Id = item.Id,
            Sku = item.Sku,
            Name = item.Name,
            Category = item.Category,
            Price = item.Price,
            IconEmoji = item.IconEmoji,
            PreviewUrl = item.PreviewUrl,
            IsOwned = true,
            IsEquipped = isEquipped,
        }, "Mua thành công!");
    }

    public async Task<ApiResponse<CompanionStateDto>> EquipAsync(Guid userId, EquipCompanionItemDto dto)
    {
        var item = await _db.CompanionCatalogItems.FirstOrDefaultAsync(c => c.Sku == dto.Sku);
        if (item == null)
        {
            return ApiResponse<CompanionStateDto>.Fail("Không tìm thấy vật phẩm.", ErrorCode.VALIDATION_ERROR);
        }

        var inv = await _db.CompanionInventories.FirstOrDefaultAsync(i =>
            i.UserId == userId && i.CatalogItemId == item.Id);
        if (inv == null)
        {
            return ApiResponse<CompanionStateDto>.Fail("Bạn chưa sở hữu vật phẩm này.", ErrorCode.VALIDATION_ERROR);
        }

        var sameCategory = await _db.CompanionInventories
            .Include(i => i.CatalogItem)
            .Where(i => i.UserId == userId && i.CatalogItem.Category == item.Category)
            .ToListAsync();
        foreach (var other in sameCategory)
        {
            other.IsEquipped = other.CatalogItemId == item.Id;
        }

        var pet = await GetOrCreatePetAsync(userId);
        if (item.Category == "background")
        {
            var theme = RoomThemeForBackgroundSku(item.Sku);
            if (theme != null)
            {
                pet.RoomTheme = theme;
            }
        }

        var equippedSkus = await _db.CompanionInventories
            .Include(i => i.CatalogItem)
            .Where(i => i.UserId == userId && i.IsEquipped)
            .Select(i => i.CatalogItem.Sku)
            .ToListAsync();
        pet.EquippedItemIds = JsonSerializer.Serialize(equippedSkus);
        pet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<CompanionStateDto>.Ok(MapState(pet), "Đã trang trí phòng!");
    }

    public async Task<ApiResponse<CompanionStateDto>> SetRoomThemeAsync(Guid userId, SetRoomThemeDto dto)
    {
        await EnsureSeedDataAsync();
        var theme = NormalizeRoomTheme(dto.Theme);
        var allowed = new[] { "room_1", "room_2", "room_3", "room_4" };
        if (!allowed.Contains(theme))
        {
            return ApiResponse<CompanionStateDto>.Fail("Chủ đề phòng không hợp lệ.", ErrorCode.VALIDATION_ERROR);
        }

        if (!await UserOwnsRoomAsync(userId, theme))
        {
            return ApiResponse<CompanionStateDto>.Fail(
                "Bạn chưa mở khóa phòng này. Mua tại cửa hàng.",
                ErrorCode.VALIDATION_ERROR);
        }

        var pet = await GetOrCreatePetAsync(userId);
        pet.RoomTheme = theme;
        await SyncBackgroundEquipAsync(userId, theme);
        pet.EquippedItemIds = JsonSerializer.Serialize(await GetEquippedSkusAsync(userId));
        pet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResponse<CompanionStateDto>.Ok(MapState(pet));
    }

    public Task ReportEventAsync(Guid userId, string eventCode, int increment = 1)
        => ReportEventInternalAsync(userId, eventCode, increment, isRetry: false);

    private async Task ReportEventInternalAsync(Guid userId, string eventCode, int increment, bool isRetry)
    {
        await EnsureSeedDataAsync();
        var templates = await _db.CompanionMissionTemplates
            .Where(t => t.IsActive && t.Code == eventCode)
            .ToListAsync();
        if (!templates.Any()) return;

        var pet = await _db.UserCompanions.FirstOrDefaultAsync(p => p.UserId == userId);
        foreach (var template in templates)
        {
            var dateKey = MissionDateKey(template.Category);
            var progress = await GetOrCreateMissionProgressAsync(userId, template.Id, dateKey);
            if (progress.CompletedAt != null) continue;

            if (eventCode == "happiness_80")
            {
                progress.Progress = pet != null && pet.Happiness >= 80 ? 1 : 0;
            }
            else
            {
                progress.Progress += increment;
            }

            progress.UpdatedAt = DateTime.UtcNow;
            if (progress.Progress >= template.TargetCount)
            {
                progress.Progress = template.TargetCount;
                progress.CompletedAt = DateTime.UtcNow;
                if (pet != null)
                {
                    pet.Coins += template.RewardCoins;
                    AddXp(pet, template.RewardXp);
                    pet.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && !isRetry)
        {
            _logger.LogWarning(ex, "Companion mission progress race for user {UserId}, event {EventCode}. Retrying once.", userId, eventCode);
            foreach (var entry in _db.ChangeTracker.Entries<CompanionMissionProgress>()
                         .Where(e => e.State == EntityState.Added)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }

            await ReportEventInternalAsync(userId, eventCode, increment, isRetry: true);
        }
    }

    private async Task SyncHappinessMissionAsync(Guid userId, int happiness)
    {
        if (happiness >= 80)
        {
            await ReportEventAsync(userId, "happiness_80");
        }
    }

    private async Task<UserCompanion> GetOrCreatePetAsync(Guid userId)
    {
        var pet = await _db.UserCompanions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pet != null)
        {
            await MigrateRoomThemeIfNeededAsync(pet);
            return pet;
        }

        pet = new UserCompanion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Level = 1,
            Xp = 0,
            Coins = 100,
            Hunger = 70,
            Happiness = 80,
            Energy = 90,
            RoomTheme = "room_1",
            EquippedItemIds = "[]",
            LastDecayAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.UserCompanions.Add(pet);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _db.Entry(pet).State = EntityState.Detached;
            return await _db.UserCompanions.FirstAsync(p => p.UserId == userId);
        }

        await GrantDefaultInventoryAsync(userId);
        await _db.SaveChangesAsync();
        return pet;
    }

    private void ApplyDecay(UserCompanion pet)
    {
        var now = DateTime.UtcNow;
        var hours = (now - pet.LastDecayAt).TotalHours;
        if (hours < 0.5) return;

        pet.Hunger = Math.Max(0, pet.Hunger - (int)(hours * 1.25));
        pet.Happiness = Math.Max(0, pet.Happiness - (int)(hours * 0.5));
        pet.Energy = Math.Max(0, pet.Energy - (int)(hours * 1.0));
        pet.LastDecayAt = now;
    }

    private static (bool ok, string? reason) CanFeed(UserCompanion pet)
    {
        if (pet.Hunger >= 90) return (false, "Mèo Xanh no rồi, chưa cần ăn thêm.");
        if (pet.Coins < FeedCost) return (false, $"Cần {FeedCost} xu để cho ăn.");
        if (pet.LastFeedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - pet.LastFeedAt.Value;
            if (elapsed.TotalMinutes < FeedCooldownMinutes)
            {
                var sec = (int)(FeedCooldownMinutes * 60 - elapsed.TotalSeconds);
                return (false, $"Chờ {sec / 60}:{(sec % 60).ToString().PadLeft(2, '0')} nữa mới cho ăn được.");
            }
        }
        return (true, null);
    }

    private static (bool ok, string? reason) CanPet(UserCompanion pet)
    {
        if (pet.Energy < 5) return (false, "Mèo Xanh mệt quá, cần nghỉ trước.");
        if (pet.LastPetAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - pet.LastPetAt.Value;
            if (elapsed.TotalMinutes < PetCooldownMinutes)
            {
                var sec = (int)(PetCooldownMinutes * 60 - elapsed.TotalSeconds);
                return (false, $"Chờ {sec} giây nữa nhé.");
            }
        }
        return (true, null);
    }

    private CompanionStateDto MapState(UserCompanion pet)
    {
        var (canFeed, feedReason) = CanFeed(pet);
        var (canPet, petReason) = CanPet(pet);
        var feedCd = 0;
        var petCd = 0;
        if (pet.LastFeedAt.HasValue)
        {
            feedCd = Math.Max(0, (int)(FeedCooldownMinutes * 60 - (DateTime.UtcNow - pet.LastFeedAt.Value).TotalSeconds));
        }
        if (pet.LastPetAt.HasValue)
        {
            petCd = Math.Max(0, (int)(PetCooldownMinutes * 60 - (DateTime.UtcNow - pet.LastPetAt.Value).TotalSeconds));
        }

        List<string> equipped;
        try
        {
            equipped = JsonSerializer.Deserialize<List<string>>(pet.EquippedItemIds) ?? new List<string>();
        }
        catch
        {
            equipped = new List<string>();
        }

        return new CompanionStateDto
        {
            Level = pet.Level,
            Xp = pet.Xp,
            XpForNextLevel = XpForNextLevel(pet.Level),
            Coins = pet.Coins,
            Hunger = pet.Hunger,
            Happiness = pet.Happiness,
            Energy = pet.Energy,
            RoomTheme = NormalizeRoomTheme(pet.RoomTheme),
            EquippedItemSkus = equipped,
            CanFeed = canFeed,
            CanPet = canPet,
            FeedBlockedReason = feedReason,
            PetBlockedReason = petReason,
            FeedCooldownSeconds = feedCd,
            PetCooldownSeconds = petCd,
            MascotMood = ResolveMood(pet),
        };
    }

    private static string ResolveMood(UserCompanion pet)
    {
        if (pet.Hunger < 30) return "hungry";
        if (pet.Happiness >= 80) return "happy";
        if (pet.Energy < 25) return "sleepy";
        if (pet.Happiness < 40) return "sad";
        return "idle";
    }

    private static void AddXp(UserCompanion pet, int amount)
    {
        pet.Xp += amount;
        while (pet.Level < LevelXpThresholds.Length &&
               pet.Xp >= LevelXpThresholds[pet.Level])
        {
            pet.Level++;
        }
    }

    private static int XpForNextLevel(int level)
    {
        if (level >= LevelXpThresholds.Length)
        {
            return LevelXpThresholds[^1] + (level - LevelXpThresholds.Length + 1) * 700;
        }
        return LevelXpThresholds[level];
    }

    private static string MissionDateKey(string category) => category switch
    {
        "daily" => TodayKey(),
        "weekly" => WeekKey(),
        _ => "lifetime",
    };

    private static string WeekKey()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return $"{vn.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(vn):00}";
    }

    private static string TodayKey()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return vn.ToString("yyyy-MM-dd");
    }

    private static string NormalizeCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "weekly" or "hangtuan" => "weekly",
            "once" or "motlan" => "once",
            _ => "daily",
        };
    }

    private async Task GrantDefaultInventoryAsync(Guid userId)
    {
        var defaults = await _db.CompanionCatalogItems.Where(c => c.IsDefaultOwned).ToListAsync();
        var ownedIds = await _db.CompanionInventories
            .Where(i => i.UserId == userId)
            .Select(i => i.CatalogItemId)
            .ToListAsync();
        var ownedSet = ownedIds.ToHashSet();
        foreach (var pending in _db.ChangeTracker.Entries<CompanionInventory>()
                     .Where(e => e.State == EntityState.Added && e.Entity.UserId == userId)
                     .Select(e => e.Entity.CatalogItemId))
        {
            ownedSet.Add(pending);
        }

        foreach (var item in defaults)
        {
            if (ownedSet.Contains(item.Id)) continue;
            _db.CompanionInventories.Add(new CompanionInventory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CatalogItemId = item.Id,
                IsEquipped = item.Sku is "plant_green" or "sofa_blue" or "bg_cozy",
                AcquiredAt = DateTime.UtcNow,
            });
            ownedSet.Add(item.Id);
        }
    }

    private async Task<CompanionMissionProgress> GetOrCreateMissionProgressAsync(
        Guid userId, Guid templateId, string dateKey)
    {
        var progress = await _db.CompanionMissionProgresses.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.TemplateId == templateId && p.DateKey == dateKey);
        if (progress != null) return progress;

        progress = _db.ChangeTracker.Entries<CompanionMissionProgress>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .FirstOrDefault(p => p.UserId == userId && p.TemplateId == templateId && p.DateKey == dateKey);
        if (progress != null) return progress;

        progress = new CompanionMissionProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TemplateId = templateId,
            DateKey = dateKey,
            Progress = 0,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.CompanionMissionProgresses.Add(progress);
        return progress;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505", StringComparison.Ordinal)
               || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRoomTheme(string theme) => theme switch
    {
        "cozy" => "room_1",
        "modern" => "room_2",
        "nature" => "room_3",
        _ => theme,
    };

    private async Task MigrateRoomThemeIfNeededAsync(UserCompanion pet)
    {
        var normalized = NormalizeRoomTheme(pet.RoomTheme);
        if (normalized == pet.RoomTheme) return;

        pet.RoomTheme = normalized;
        pet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static string? BackgroundSkuForRoom(string roomTheme) =>
        NormalizeRoomTheme(roomTheme) switch
        {
            "room_1" => "bg_cozy",
            "room_2" => "bg_modern",
            "room_3" => "bg_nature",
            "room_4" => "bg_room_4",
            _ => null,
        };

    private static string? RoomThemeForBackgroundSku(string sku) => sku switch
    {
        "bg_cozy" => "room_1",
        "bg_modern" => "room_2",
        "bg_nature" => "room_3",
        "bg_room_4" => "room_4",
        _ => null,
    };

    private async Task<bool> UserOwnsRoomAsync(Guid userId, string roomTheme)
    {
        var normalized = NormalizeRoomTheme(roomTheme);
        if (normalized == "room_1") return true;

        var sku = BackgroundSkuForRoom(normalized);
        if (sku == null) return false;

        var item = await _db.CompanionCatalogItems.FirstOrDefaultAsync(c => c.Sku == sku);
        if (item == null) return false;

        return await _db.CompanionInventories.AnyAsync(i =>
            i.UserId == userId && i.CatalogItemId == item.Id);
    }

    private async Task SyncBackgroundEquipAsync(Guid userId, string roomTheme)
    {
        var sku = BackgroundSkuForRoom(roomTheme);
        if (sku == null) return;

        var target = await _db.CompanionCatalogItems.FirstOrDefaultAsync(c => c.Sku == sku);
        if (target == null) return;

        var inventories = await _db.CompanionInventories
            .Include(i => i.CatalogItem)
            .Where(i => i.UserId == userId && i.CatalogItem!.Category == "background")
            .ToListAsync();

        foreach (var inv in inventories)
        {
            inv.IsEquipped = inv.CatalogItemId == target.Id;
        }
    }

    private async Task<List<string>> GetEquippedSkusAsync(Guid userId) =>
        await _db.CompanionInventories
            .Include(i => i.CatalogItem)
            .Where(i => i.UserId == userId && i.IsEquipped)
            .Select(i => i.CatalogItem!.Sku)
            .ToListAsync();

    private async Task EnsureSeedDataAsync()
    {
        if (await _db.CompanionCatalogItems.AnyAsync())
        {
            await EnsureCatalogExtrasAsync();
            return;
        }

        var catalog = new List<CompanionCatalogItem>
        {
            new() { Id = Guid.NewGuid(), Sku = "plant_green", Name = "Cây xanh", Category = "furniture", Price = 0, IconEmoji = "🪴", IsDefaultOwned = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "desk_books", Name = "Bàn học", Category = "furniture", Price = 50, IconEmoji = "📚", IsDefaultOwned = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "sofa_blue", Name = "Ghế sofa", Category = "furniture", Price = 80, IconEmoji = "🛋️", IsDefaultOwned = true, SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "lamp_warm", Name = "Đèn bàn", Category = "furniture", Price = 40, IconEmoji = "💡", IsDefaultOwned = true, SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "bg_cozy", Name = "Phòng 1", Category = "background", Price = 0, IconEmoji = "🏠", IsDefaultOwned = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "bg_modern", Name = "Phòng 2", Category = "background", Price = 120, IconEmoji = "🏢", SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "bg_nature", Name = "Phòng 3", Category = "background", Price = 120, IconEmoji = "🌿", SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "bg_room_4", Name = "Phòng 4", Category = "background", Price = 150, IconEmoji = "✨", SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Sku = "outfit_scarf", Name = "Khăn xanh", Category = "outfit", Price = 60, IconEmoji = "🧣", SortOrder = 1, CreatedAt = DateTime.UtcNow },
        };
        _db.CompanionCatalogItems.AddRange(catalog);

        var missions = new List<CompanionMissionTemplate>
        {
            new() { Id = Guid.NewGuid(), Code = "daily_login", Title = "Đăng nhập app", Description = "Mở HealthPath hôm nay", Category = "daily", TargetCount = 1, RewardCoins = 5, RewardXp = 10, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), Code = "feed_pet", Title = "Cho ăn 1 lần", Description = "Cho bạn đồng hành ăn", Category = "daily", TargetCount = 1, RewardCoins = 5, RewardXp = 10, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), Code = "pet_interact", Title = "Vuốt ve 5 lần", Description = "Tương tác với mèo Xanh", Category = "daily", TargetCount = 5, RewardCoins = 10, RewardXp = 15, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), Code = "routine_complete", Title = "Hoàn thành 1 thói quen", Description = "Làm ít nhất 1 routine hôm nay", Category = "daily", TargetCount = 1, RewardCoins = 15, RewardXp = 20, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), Code = "audio_listen", Title = "Nghe 2 bài audio", Description = "Nghe nhạc thư giãn", Category = "daily", TargetCount = 2, RewardCoins = 10, RewardXp = 15, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), Code = "group_checkin", Title = "Điểm danh nhóm", Description = "Check-in cùng nhóm hôm nay", Category = "daily", TargetCount = 1, RewardCoins = 20, RewardXp = 25, SortOrder = 6 },
            new() { Id = Guid.NewGuid(), Code = "happiness_80", Title = "Hạnh phúc > 80", Description = "Duy trì vui vẻ > 80", Category = "daily", TargetCount = 1, RewardCoins = 15, RewardXp = 20, SortOrder = 7 },
            new() { Id = Guid.NewGuid(), Code = "weekly_routine_5", Title = "5 thói quen/tuần", Description = "Hoàn thành 5 routine trong tuần", Category = "weekly", TargetCount = 5, RewardCoins = 50, RewardXp = 80, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), Code = "once_level_5", Title = "Đạt level 5", Description = "Nâng cấp bạn đồng hành lên level 5", Category = "once", TargetCount = 1, RewardCoins = 200, RewardXp = 500, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), Code = "once_coins_1000", Title = "Tích lũy 1000 xu", Description = "Tích lũy tổng cộng 1000 xu", Category = "once", TargetCount = 1000, RewardCoins = 500, RewardXp = 1000, SortOrder = 2 },
        };
        _db.CompanionMissionTemplates.AddRange(missions);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Companion catalog and missions seeded.");
        await EnsureCatalogExtrasAsync();
    }

    private async Task EnsureCatalogExtrasAsync()
    {
        if (await _db.CompanionCatalogItems.AnyAsync(c => c.Sku == "bg_room_4")) return;

        _db.CompanionCatalogItems.Add(new CompanionCatalogItem
        {
            Id = Guid.NewGuid(),
            Sku = "bg_room_4",
            Name = "Phòng 4",
            Category = "background",
            Price = 150,
            IconEmoji = "✨",
            SortOrder = 4,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("Companion catalog: added bg_room_4.");
    }
}
