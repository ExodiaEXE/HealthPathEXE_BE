using System;

namespace HealthPath.API.Models;

public class RecurringTemplate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoutineId { get; set; }
    public string DaysOfWeek { get; set; } = null!; // JSON array: [1,2,3]
    public TimeOnly ScheduledTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Routine Routine { get; set; } = null!;
}
