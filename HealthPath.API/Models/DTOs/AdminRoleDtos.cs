using System;
using System.Collections.Generic;

namespace HealthPath.API.Models.DTOs;

public class AdminRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPermissionDto
{
    public Guid Id { get; set; }
    public string Resource { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? Description { get; set; }
}

public class AssignPermissionDto
{
    public Guid RoleId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class RoleWithPermissionsDto : AdminRoleDto
{
    public List<AdminPermissionDto> Permissions { get; set; } = new();
}
