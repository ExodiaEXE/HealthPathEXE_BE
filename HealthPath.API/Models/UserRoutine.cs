using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class UserRoutine
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RoutineId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public int ElapsedSeconds { get; set; } = 0;

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Routine Routine { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
