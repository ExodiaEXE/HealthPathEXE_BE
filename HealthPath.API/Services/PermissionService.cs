using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HealthPath.API.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string roleName, string permissionAction);
}

public class PermissionService : IPermissionService
{
    private readonly HealthpathDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionService(HealthpathDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(string roleName, string permissionAction)
    {
        if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(permissionAction))
            return false;

        // Bỏ qua check quyền nếu là SuperAdmin
        if (roleName == "SuperAdmin")
            return true;

        var cacheKey = $"RolePermissions_{roleName}";

        if (!_cache.TryGetValue(cacheKey, out string[]? allowedActions))
        {
            allowedActions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role.Name == roleName)
                .Select(rp => rp.Permission.Action)
                .ToArrayAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(System.TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, allowedActions, cacheOptions);
        }

        return allowedActions != null && allowedActions.Contains(permissionAction);
    }
}
