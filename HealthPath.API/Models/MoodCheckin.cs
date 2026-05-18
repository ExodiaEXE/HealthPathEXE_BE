using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class MoodCheckin
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Mood { get; set; } = null!;

    public string EnergyLevel { get; set; } = null!;

    public int StreakDay { get; set; }

    public string? Note { get; set; }

    public DateTime CheckedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
