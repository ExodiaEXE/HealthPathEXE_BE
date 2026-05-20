using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

public class RoutineDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public bool IsPremium { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRoutineDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Difficulty { get; set; } = "easy"; // easy, medium, hard

    public int DurationMinutes { get; set; } = 10;
    
    public bool IsPremium { get; set; } = false;
    
    public string? ThumbnailUrl { get; set; }
}

public class UpdateRoutineDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(20)]
    public string? Difficulty { get; set; }

    public int? DurationMinutes { get; set; }
    
    public bool? IsPremium { get; set; }
    
    public string? ThumbnailUrl { get; set; }
}
