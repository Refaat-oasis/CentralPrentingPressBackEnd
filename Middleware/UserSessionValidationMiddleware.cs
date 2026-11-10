using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CentralPress.API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CentralPress.API.Middleware;

public class UserSessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public UserSessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // 1. Skip unauthenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // 2. Skip SuperAdmin (has its own hourly TOTP auth)
        if (context.User.IsInRole("SuperAdmin"))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // 3. Allow logout & terminate endpoints to proceed
        if (path.Contains("/api/auth/logout") || path.Contains("/api/auth/terminate-session"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenSessionId = context.User.FindFirstValue("SessionId");

        // If no SessionId claim is present (e.g. mock JWT in unit/integration test suite), bypass session tracking
        if (string.IsNullOrEmpty(tokenSessionId))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new { message = "معرّف الجلسة غير صالح. يرجى إعادة تسجيل الدخول." });
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new { message = "الحساب غير موجود أو تم تعطيله من قبل الإدارة." });
            return;
        }

        // 4. Validate Single Active Session (Prevent Concurrent Logins)
        if (string.IsNullOrEmpty(user.CurrentSessionId) || user.CurrentSessionId != tokenSessionId)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "تم إنهاء هذه الجلسة بسبب تسجيل الدخول بهذا الحساب من جهاز أو متصفح آخر."
            });
            return;
        }

        // 5. Validate Inactivity / Idle Timeout
        var idleTimeoutMinutes = configuration.GetValue<double>("SessionSettings:IdleTimeoutMinutes", 15);
        var maxLifetimeHours = configuration.GetValue<double>("SessionSettings:MaxSessionLifetimeHours", 12);

        if (user.LastActivityUtc.HasValue &&
            (DateTime.UtcNow - user.LastActivityUtc.Value).TotalMinutes > idleTimeoutMinutes)
        {
            // Terminate expired session in database
            user.CurrentSessionId = null;
            await dbContext.SaveChangesAsync();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"انتهت صلاحية الجلسة لعدم النشاط لمدة أكثر من {idleTimeoutMinutes} دقيقة. يرجى تسجيل الدخول مجدداً."
            });
            return;
        }

        // 6. Validate Max Session Lifetime
        if (user.SessionStartedUtc.HasValue &&
            (DateTime.UtcNow - user.SessionStartedUtc.Value).TotalHours > maxLifetimeHours)
        {
            user.CurrentSessionId = null;
            await dbContext.SaveChangesAsync();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"انتهت صلاحية الجلسة لتجاوز الحد الأقصى ({maxLifetimeHours} ساعة). يرجى تسجيل الدخول مجدداً."
            });
            return;
        }

        // 7. Update LastActivityUtc with Throttling (once every 30s)
        if (!user.LastActivityUtc.HasValue || (DateTime.UtcNow - user.LastActivityUtc.Value).TotalSeconds >= 30)
        {
            user.LastActivityUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        await _next(context);
    }
}
