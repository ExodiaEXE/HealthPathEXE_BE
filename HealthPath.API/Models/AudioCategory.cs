using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class AudioCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AudioTrack> AudioTracks { get; set; } = new List<AudioTrack>();
}
