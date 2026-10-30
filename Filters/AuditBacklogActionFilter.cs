using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CentralPress.API.Filters;

public class AuditBacklogActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;

        // Execute action
        var executedContext = await next();

        // Audit only modifying actions or significant calls
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

                // Determine action type from path and method
                var path = httpContext.Request.Path.Value ?? "";
                string actionType = method;
                if (path.Contains("toggle-active")) actionType = "ToggleActive";
                else if (path.Contains("confirm")) actionType = "ConfirmPaymentOrder";
                else if (path.Contains("export-word")) actionType = "ExportWord";
                else if (path.Contains("export-pdf")) actionType = "ExportPdf";
                else if (path.Contains("login")) actionType = "Login";

                // Module name from path
                string moduleName = "System";
                if (path.Contains("/moqaysa")) moduleName = "MOQAYSA";
                else if (path.Contains("/tasweya")) moduleName = "TASWEYA";
                else if (path.Contains("/payment-orders")) moduleName = "PAYMENT_ORDERS";
                else if (path.Contains("/admin-users")) moduleName = "EMPLOYEES";
                else if (path.Contains("/job-titles")) moduleName = "JOB_TITLES";
                else if (path.Contains("/machineries")) moduleName = "MACHINERY";
                else if (path.Contains("/materials")) moduleName = "MATERIALS";
                else if (path.Contains("/customers")) moduleName = "CUSTOMERS";
                else if (path.Contains("/reports")) moduleName = "REPORTS";

                string? payload = null;
                if (context.ActionArguments.Count > 0)
                {
                    try
                    {
                        payload = JsonSerializer.Serialize(context.ActionArguments);
                    }
                    catch
                    {
                        // Ignore serialization issues
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
}
