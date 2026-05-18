using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class RolePermission
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
