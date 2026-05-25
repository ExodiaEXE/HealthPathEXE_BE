using System;

namespace HealthPath.API.Models;

public partial class UserFavoriteTrack
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TrackId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AudioTrack Track { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
