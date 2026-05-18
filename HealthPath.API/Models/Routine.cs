using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class Routine
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public int DurationMinutes { get; set; }

    public string Difficulty { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }

    public bool IsSystem { get; set; }

    public bool IsPremium { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<UserRoutine> UserRoutines { get; set; } = new List<UserRoutine>();
}
