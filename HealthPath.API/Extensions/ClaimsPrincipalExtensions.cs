using System;
using System.Security.Claims;

namespace HealthPath.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            // User JWT dùng "sub"; admin JWT (.NET) thường serialize NameIdentifier thành "nameid"
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value
                        ?? user.FindFirst("nameid")?.Value;

            if (string.IsNullOrEmpty(value))
                throw new UnauthorizedAccessException("Token hợp lệ nhưng không chứa thông tin định danh (sub/nameid)!");

            return Guid.Parse(value);
        }

        public static string? GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirst("Role")?.Value;
        }

        public static bool IsSuperAdmin(this ClaimsPrincipal user)
        {
            return GetRole(user) == "SuperAdmin";
        }

        public static bool IsAdminToken(this ClaimsPrincipal user)
        {
            return user.FindFirst("IsAdmin")?.Value == "true";
        }
    }
}
