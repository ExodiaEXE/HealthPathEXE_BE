using System;

namespace HealthPath.API.Models;

public class UserStats
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int StreakCurrent { get; set; }
    public int StreakBest { get; set; }
    public DateOnly? StreakUpdatedDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
