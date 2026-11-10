using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CentralPress.API.Common;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CentralPress.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string ModuleKey { get; }
    public string ActionType { get; }

    public PermissionAuthorizeAttribute(string moduleKey, string actionType)
    {
        ModuleKey = moduleKey;
        ActionType = actionType;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // SuperAdmin or Admin role bypasses granular permission checks
        if (user.IsInRole("SuperAdmin") || user.IsInRole("Admin"))
        {
            return;
        }

        var normalizedClaim = PermissionConstants.CreatePermission(ModuleKey, ActionType);
        var legacyPolicyName = PermissionConstants.LegacyPolicyName(ModuleKey, ActionType);

        // 1. Fast path: check in-memory JWT claims
        bool hasClaim = user.HasClaim(c =>
            c.Type == PermissionConstants.ClaimType &&
            (c.Value.Equals(normalizedClaim, StringComparison.OrdinalIgnoreCase) ||
             c.Value.Equals(legacyPolicyName, StringComparison.OrdinalIgnoreCase) ||
             c.Value.Equals($"{ModuleKey}:{ActionType}", StringComparison.OrdinalIgnoreCase)));

        if (hasClaim)
        {
            return;
        }

        // 2. Fallback check against PermissionService (for real-time database overrides)
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var permService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();
            bool hasDbPerm = await permService.HasPermissionAsync(userId, ModuleKey, ActionType);
            if (hasDbPerm)
            {
                return;
            }
        }

        context.Result = new ObjectResult(new { message = $"ليس لديك صلاحية ({ActionType}) على وحدة ({ModuleKey})" })
        {
            StatusCode = 403
        };
    }
}
