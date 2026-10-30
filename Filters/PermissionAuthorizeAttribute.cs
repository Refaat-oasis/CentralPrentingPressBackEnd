using System;
using System.Security.Claims;
using System.Threading.Tasks;
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

        // SuperAdmin bypass
        if (user.IsInRole("SuperAdmin"))
        {
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();
        bool hasPerm = await permService.HasPermissionAsync(userId, ModuleKey, ActionType);

        if (!hasPerm)
        {
            context.Result = new ObjectResult(new { message = $"ليس لديك صلاحية ({ActionType}) على وحدة ({ModuleKey})" })
            {
                StatusCode = 403
            };
        }
    }
}
