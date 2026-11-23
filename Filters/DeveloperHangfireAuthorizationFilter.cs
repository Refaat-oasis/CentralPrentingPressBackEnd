using Hangfire.Dashboard;

namespace CentralPress.API.Filters;

/// <summary>
/// Restricts Hangfire's built-in /hangfire dashboard exclusively to users with the Developer role.
/// </summary>
public sealed class DeveloperHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Developer");
    }
}
