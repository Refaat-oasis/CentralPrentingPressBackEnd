using System;

namespace CentralPress.API.ViewModels;

public class AuditBacklogViewModel
{
    public long AuditId { get; init; }
    public string? UserId { get; init; }
    public string? EmployeeName { get; init; }
    public string? JobTitleName { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string? ModuleName { get; init; }
    public string? EntityName { get; init; }
    public string? RecordId { get; init; }
    public string? RequestPath { get; init; }
    public string? OldValuesJson { get; init; }
    public string? NewValuesJson { get; init; }
    public string? IpAddress { get; init; }
    public DateTime TimestampUtc { get; init; }
}
