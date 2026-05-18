using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class ChallengeParticipant
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public Guid UserId { get; set; }

    public int Score { get; set; }

    public string Status { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual GroupChallenge Challenge { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
