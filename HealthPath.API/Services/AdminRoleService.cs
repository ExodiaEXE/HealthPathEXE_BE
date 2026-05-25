using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HealthPath.API.Services;

public interface IAdminRoleService
{
    Task<ApiResponse<List<AdminRoleDto>>> GetRolesAsync();
    Task<ApiResponse<List<AdminPermissionDto>>> GetPermissionsAsync();
    Task<ApiResponse<RoleWithPermissionsDto>> GetRoleWithPermissionsAsync(Guid roleId);
    Task<ApiResponse<bool>> AssignPermissionsToRoleAsync(AssignPermissionDto request);
}

public class AdminRoleService : IAdminRoleService
{
    private readonly HealthpathDbContext _context;
    private readonly IMemoryCache _cache;

    public AdminRoleService(HealthpathDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ApiResponse<List<AdminRoleDto>>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .Where(r => r.DeletedAt == null)
            .Select(r => new AdminRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<AdminRoleDto>>.Ok(roles);
    }

    public async Task<ApiResponse<List<AdminPermissionDto>>> GetPermissionsAsync()
    {
        var permissions = await _context.Permissions
            .Select(p => new AdminPermissionDto
            {
                Id = p.Id,
                Resource = p.Resource,
                Action = p.Action,
                Description = p.Description
            })
            .ToListAsync();

        return ApiResponse<List<AdminPermissionDto>>.Ok(permissions);
    }

    public async Task<ApiResponse<RoleWithPermissionsDto>> GetRoleWithPermissionsAsync(Guid roleId)
    {
        var role = await _context.Roles
            .Where(r => r.Id == roleId && r.DeletedAt == null)
            .Select(r => new RoleWithPermissionsDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt,
                Permissions = r.RolePermissions.Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Resource = rp.Permission.Resource,
                    Action = rp.Permission.Action,
                    Description = rp.Permission.Description
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (role == null)
        {
            return ApiResponse<RoleWithPermissionsDto>.Fail("Không tìm thấy vai trò.", ErrorCode.ROLE_NOT_FOUND);
        }

        return ApiResponse<RoleWithPermissionsDto>.Ok(role);
    }

    public async Task<ApiResponse<bool>> AssignPermissionsToRoleAsync(AssignPermissionDto request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.DeletedAt == null);

        if (role == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy vai trò.", ErrorCode.ROLE_NOT_FOUND);
        }

        // Không cho phép sửa quyền của SuperAdmin (nếu cần bảo vệ cứng)
        if (role.Name == "SuperAdmin")
        {
            return ApiResponse<bool>.Fail("Không thể chỉnh sửa quyền của SuperAdmin.", ErrorCode.FORBIDDEN);
        }

        // Xóa các quyền cũ
        _context.RolePermissions.RemoveRange(role.RolePermissions);

        // Thêm quyền mới
        var newRolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.RolePermissions.AddRangeAsync(newRolePermissions);
        await _context.SaveChangesAsync();

        // Clear cache để User load lại quyền mới ở request tiếp theo
        _cache.Remove($"RolePermissions_{role.Name}");

        return ApiResponse<bool>.Ok(true, "Cập nhật quyền thành công.");
    }
}
