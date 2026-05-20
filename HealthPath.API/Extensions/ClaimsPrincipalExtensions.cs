using System;
using System.Security.Claims;

namespace HealthPath.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? user.FindFirst("sub")?.Value;

            // Nếu lọt qua [Authorize] mà vẫn không có sub (lỗi cấu hình token) thì ném ra ngoại lệ luôn
            if (string.IsNullOrEmpty(value))
                throw new UnauthorizedAccessException("Token hợp lệ nhưng không chứa thông tin định danh (Sub/NameIdentifier)!");

            return Guid.Parse(value);
        }
    }
}
