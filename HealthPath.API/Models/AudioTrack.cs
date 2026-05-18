using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class AudioTrack
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Artist { get; set; }

    public string? Studio { get; set; }

    public string Category { get; set; } = null!;

    public int DurationSeconds { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? CoverUrl { get; set; }

    public bool IsPremium { get; set; }

    public bool IsActive { get; set; }

    public long PlayCount { get; set; }

    public Guid? UploadedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? UploadedByNavigation { get; set; }

    public virtual ICollection<UserAudioHistory> UserAudioHistories { get; set; } = new List<UserAudioHistory>();
}
