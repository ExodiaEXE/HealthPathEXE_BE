using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class UserRoutineDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoutineId { get; set; }
    public string Status { get; set; } = null!; // pending, in_progress, completed, failed, cancelled
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public int ElapsedSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public RoutineDto? Routine { get; set; }
}

public class CreateUserRoutineDto
{
    [Required]
    public Guid RoutineId { get; set; }

    public DateTime? ScheduledAt { get; set; }
}

public class UserRoutineStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = null!; // "in_progress", "completed", "failed", "cancelled"
    
    public int? ElapsedSeconds { get; set; } // for "in_progress" or "completed" or "failed" to track progress
    
    public int? ActualDurationMinutes { get; set; } // mainly for "completed"
}

public class UserStatsDto
{
    public int StreakCurrent { get; set; }
    public int StreakBest { get; set; }
    public DateOnly? StreakUpdatedDate { get; set; }
}

public class CreateRecurringTemplateDto
{
    [Required]
    public Guid RoutineId { get; set; }

    [Required]
    public System.Collections.Generic.List<int> DaysOfWeek { get; set; } = null!; // [1, 2, 3...]

    [Required]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "ScheduledTime must be in HH:mm:ss format.")]
    public string ScheduledTime { get; set; } = null!; // "HH:mm:ss"
}

public class RecurringTemplateDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoutineId { get; set; }
    public System.Collections.Generic.List<int> DaysOfWeek { get; set; } = null!;
    public string ScheduledTime { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public RoutineDto? Routine { get; set; }
}

