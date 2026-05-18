using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class UserAudioHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TrackId { get; set; }

    public int PlayedSeconds { get; set; }

    public DateTime PlayedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual AudioTrack Track { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
