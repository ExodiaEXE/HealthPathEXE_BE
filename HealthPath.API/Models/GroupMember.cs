using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class GroupMember
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
