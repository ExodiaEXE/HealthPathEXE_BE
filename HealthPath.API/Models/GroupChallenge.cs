using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class GroupChallenge
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChallengeParticipant> ChallengeParticipants { get; set; } = new List<ChallengeParticipant>();

    public virtual Group Group { get; set; } = null!;
}
