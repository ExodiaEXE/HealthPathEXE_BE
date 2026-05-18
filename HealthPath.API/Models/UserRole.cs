using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class UserRole
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User? AssignedByNavigation { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
