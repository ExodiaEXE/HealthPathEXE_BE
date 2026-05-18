using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class NotificationSetting
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool DailyCheckin { get; set; }

    public bool StreakReminder { get; set; }

    public bool GroupActivity { get; set; }

    public bool ChallengeUpdates { get; set; }

    public bool Promotions { get; set; }

    public bool PushEnabled { get; set; }

    public bool EmailEnabled { get; set; }

    public bool InAppEnabled { get; set; }

    public TimeOnly? QuietFrom { get; set; }

    public TimeOnly? QuietUntil { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
