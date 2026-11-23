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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
                var isDeveloper = httpContext.User.IsInRole("Developer");

                string? employeeName = httpContext.User.FindFirstValue("FullName");
                string? jobTitleName = httpContext.User.FindFirstValue("JobTitle");

                if (isDeveloper)
                {
                    employeeName ??= "مطور النظام (Developer)";
                    jobTitleName ??= "مطور النظام";
                }

                var path = httpContext.Request.Path.Value ?? "";
                var lowerPath = path.ToLowerInvariant();

                string actionType = method switch
                {
                    "POST" => "Create",
                    "PUT" or "PATCH" => "Update",
                    "DELETE" => "Delete",
                    _ => method
                };
                if (lowerPath.Contains("toggle-active") || lowerPath.Contains("toggle-status")) actionType = "ToggleActive";
                else if (lowerPath.Contains("confirm")) actionType = "ConfirmPaymentOrder";
                else if (lowerPath.Contains("export-word") || lowerPath.Contains("/docx")) actionType = "ExportWord";
                else if (lowerPath.Contains("export-pdf") || lowerPath.Contains("/pdf")) actionType = "ExportPdf";
                else if (lowerPath.Contains("test-smtp")) actionType = "TestSmtp";
                else if (lowerPath.Contains("requeue")) actionType = "RequeueJob";
                else if (lowerPath.Contains("/login")) actionType = "Login";
                else if (lowerPath.Contains("/logout")) actionType = "Logout";

                string moduleName = "SYSTEM";
                if (lowerPath.Contains("/moqaysa")) moduleName = "MOQAYSA";
                else if (lowerPath.Contains("/tasweya")) moduleName = "TASWEYA";
                else if (lowerPath.Contains("/payment-orders") || lowerPath.Contains("/paymentorders")) moduleName = "PAYMENT_ORDERS";
                else if (lowerPath.Contains("/admin-users") || lowerPath.Contains("/adminusers")) moduleName = "EMPLOYEES";
                else if (lowerPath.Contains("/job-titles") || lowerPath.Contains("/jobtitles")) moduleName = "JOB_TITLES";
                else if (lowerPath.Contains("/machineries")) moduleName = "MACHINERY";
                else if (lowerPath.Contains("/materials")) moduleName = "MATERIALS";
                else if (lowerPath.Contains("/customers")) moduleName = "CUSTOMERS";
                else if (lowerPath.Contains("/reports")) moduleName = "REPORTS";
                else if (lowerPath.Contains("/auth")) moduleName = "AUTH";
                else if (lowerPath.Contains("/superadmin") || lowerPath.Contains("/developer") || lowerPath.Contains("/hangfire")) moduleName = "DEVELOPER";

                string? payload = null;

                // Path-level security redaction: Auth endpoints are completely redacted
                if (path.Contains("/api/auth", StringComparison.OrdinalIgnoreCase))
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

                int statusCode = httpContext.Response.StatusCode;
                string? errorMessage = null;

                if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                {
                    statusCode = StatusCodes.Status500InternalServerError;
                    errorMessage = executedContext.Exception.Message;
                }
                else if (executedContext.Result is IStatusCodeActionResult statusResult && statusResult.StatusCode.HasValue)
                {
                    statusCode = statusResult.StatusCode.Value;
                }

                bool isSuccess = executedContext.Exception == null && statusCode < 400;
                if (!isSuccess && string.IsNullOrWhiteSpace(errorMessage) && executedContext.Result is ObjectResult objRes && objRes.Value != null)
                {
                    try
                    {
                        var node = JsonSerializer.SerializeToNode(objRes.Value);
                        if (node is JsonObject obj && obj.TryGetPropertyValue("message", out var msgNode) && msgNode != null)
                        {
                            errorMessage = msgNode.ToString();
                        }
                        else if (node != null)
                        {
                            SanitizeNode(node);
                            errorMessage = node.ToJsonString();
                        }
                    }
                    catch
                    {
                        errorMessage = $"HTTP {statusCode}";
                    }
                }

                if (!string.IsNullOrEmpty(errorMessage) && errorMessage.Length > 1000)
                {
                    errorMessage = errorMessage[..1000];
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
                    StatusCode = statusCode,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
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
