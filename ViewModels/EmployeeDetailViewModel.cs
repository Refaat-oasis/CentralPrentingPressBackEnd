using System;

namespace CentralPress.API.ViewModels;

public class EmployeeDetailViewModel
{
    public string Id { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhotoUrl { get; init; }
    public int? JobTitleId { get; init; }
    public string JobTitleName { get; init; } = "غير محدد";
    public bool IsActive { get; init; }
    public bool MustChangePassword { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
