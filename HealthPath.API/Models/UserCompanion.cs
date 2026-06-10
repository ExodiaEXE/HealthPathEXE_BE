using System;

namespace HealthPath.API.Models;

public class UserCompanion
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Level { get; set; }
    public int Xp { get; set; }
    public int Coins { get; set; }
    public int Hunger { get; set; }
    public int Happiness { get; set; }
    public int Energy { get; set; }
    public string RoomTheme { get; set; } = "cozy";
    public string EquippedItemIds { get; set; } = "[]";
    public DateTime? LastFeedAt { get; set; }
    public DateTime? LastPetAt { get; set; }
    public DateTime LastDecayAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
