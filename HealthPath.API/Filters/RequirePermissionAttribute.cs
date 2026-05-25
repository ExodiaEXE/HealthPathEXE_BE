using System;
using System.Threading.Tasks;
using HealthPath.API.Extensions;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace HealthPath.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Nếu user chưa authenticate
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var role = user.GetRole();
        if (string.IsNullOrEmpty(role))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Bỏ qua check quyền nếu là SuperAdmin
        if (role == "SuperAdmin")
        {
            return;
        }

        // Lấy PermissionService thông qua Dependency Injection
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        bool hasPermission = await permissionService.HasPermissionAsync(role, _permission);

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
