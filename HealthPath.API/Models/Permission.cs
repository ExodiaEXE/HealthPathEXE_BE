using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class Permission
{
    public Guid Id { get; set; }

    public string Resource { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
