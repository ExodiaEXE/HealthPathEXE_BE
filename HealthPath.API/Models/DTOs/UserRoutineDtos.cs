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
    public int ScoreEarned { get; set; }
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
