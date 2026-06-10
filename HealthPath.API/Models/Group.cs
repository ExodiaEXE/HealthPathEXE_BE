using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class Group
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string InviteCode { get; set; } = null!;

    public string? CoverUrl { get; set; }

    public Guid OwnerId { get; set; }

    public int MaxMembers { get; set; }

    public bool IsPublic { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<GroupChallenge> GroupChallenges { get; set; } = new List<GroupChallenge>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<GroupTeamCheckin> GroupTeamCheckins { get; set; } = new List<GroupTeamCheckin>();

    public virtual User Owner { get; set; } = null!;
}
