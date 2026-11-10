using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using Microsoft.AspNetCore.Http;

namespace CentralPress.API.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogStatusTransitionAsync(
        string moduleName,
        string entityName,
        string recordId,
        string actionType,
        object? oldValues,
        object? newValues,
        string? userId = null,
        string? employeeName = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var currentUserId = userId
            ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "System";
        var currentEmployeeName = employeeName
            ?? httpContext?.User?.FindFirst("FullName")?.Value
            ?? httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? "النظام";
        var jobTitle = httpContext?.User?.FindFirst("JobTitle")?.Value;

        var log = new AuditBacklog
        {
            UserId = currentUserId,
            EmployeeName = currentEmployeeName,
            JobTitleName = jobTitle,
            ActionType = actionType,
            ModuleName = moduleName,
            EntityName = entityName,
            RecordId = recordId,
            RequestPath = httpContext?.Request?.Path.Value ?? "",
            OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            TimestampUtc = DateTime.UtcNow
        };

        _context.AuditBacklogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }
}
