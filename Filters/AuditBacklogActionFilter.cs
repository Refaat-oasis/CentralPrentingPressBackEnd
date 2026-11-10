using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CentralPress.API.Filters;

public class AuditBacklogActionFilter : IAsyncActionFilter
{
    private static readonly string[] SensitiveKeys =
    {
        "password", "secret", "token", "code", "key", "credential", "refreshtoken", "currentpassword", "newpassword"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;

        var executedContext = await next();

        if (method is "POST" or "PUT" or "PATCH" or "DELETE" || httpContext.Request.Path.Value?.Contains("export") == true)
        {
            try
            {
                var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");

                string? employeeName = httpContext.User.FindFirstValue("FullName");
                string? jobTitleName = httpContext.User.FindFirstValue("JobTitle");

                if (isSuperAdmin)
                {
                    employeeName ??= "Super Admin (مسؤول فائق)";
                    jobTitleName ??= "Super Administrator";
                }

                var path = httpContext.Request.Path.Value ?? "";
                string actionType = method;
                if (path.Contains("toggle-active") || path.Contains("toggle-status")) actionType = "ToggleActive";
                else if (path.Contains("confirm")) actionType = "ConfirmPaymentOrder";
                else if (path.Contains("export-word")) actionType = "ExportWord";
                else if (path.Contains("export-pdf")) actionType = "ExportPdf";
                else if (path.Contains("login")) actionType = "Login";

                string moduleName = "System";
                if (path.Contains("/moqaysa")) moduleName = "MOQAYSA";
                else if (path.Contains("/tasweya")) moduleName = "TASWEYA";
                else if (path.Contains("/payment-orders") || path.Contains("/paymentorders")) moduleName = "PAYMENT_ORDERS";
                else if (path.Contains("/admin-users") || path.Contains("/adminusers")) moduleName = "EMPLOYEES";
                else if (path.Contains("/job-titles") || path.Contains("/jobtitles")) moduleName = "JOB_TITLES";
                else if (path.Contains("/machineries")) moduleName = "MACHINERY";
                else if (path.Contains("/materials")) moduleName = "MATERIALS";
                else if (path.Contains("/customers")) moduleName = "CUSTOMERS";
                else if (path.Contains("/reports")) moduleName = "REPORTS";
                else if (path.Contains("/auth")) moduleName = "AUTH";
                else if (path.Contains("/superadmin")) moduleName = "SUPERADMIN";

                string? payload = null;

                // Path-level security redaction: Auth endpoints are completely redacted
                if (path.Contains("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("/api/superadmin/login", StringComparison.OrdinalIgnoreCase))
                {
                    payload = "{\"redacted\": true, \"reason\": \"Authentication payload redacted for security\"}";
                }
                else if (context.ActionArguments.Count > 0)
                {
                    try
                    {
                        var jsonNode = JsonSerializer.SerializeToNode(context.ActionArguments);
                        if (jsonNode != null)
                        {
                            SanitizeNode(jsonNode);
                            payload = jsonNode.ToJsonString();
                        }
                    }
                    catch
                    {
                        payload = "{\"error\": \"Unable to serialize payload safely\"}";
                    }
                }

                var backlog = new AuditBacklog
                {
                    UserId = userId ?? "Anonymous",
                    EmployeeName = employeeName ?? "غير محدد",
                    JobTitleName = jobTitleName ?? "غير محدد",
                    ActionType = actionType,
                    ModuleName = moduleName,
                    RequestPath = path,
                    NewValuesJson = payload,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    TimestampUtc = DateTime.UtcNow
                };

                dbContext.AuditBacklogs.Add(backlog);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Never break user request on audit log failure
            }
        }
    }

    private static void SanitizeNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var keysToRedact = obj
                .Where(kvp => SensitiveKeys.Any(s => kvp.Key.Contains(s, StringComparison.OrdinalIgnoreCase)))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRedact)
            {
                obj[key] = "[REDACTED]";
            }

            foreach (var kvp in obj)
            {
                if (kvp.Value is JsonObject || kvp.Value is JsonArray)
                {
                    SanitizeNode(kvp.Value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                SanitizeNode(item);
            }
        }
    }
}
