namespace CentralPress.API.ViewModels;

public class UserProfileViewModel
{
    public string Id { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhotoUrl { get; init; }
    public int? JobTitleId { get; init; }
    public string? JobTitleName { get; init; }
    public bool MustChangePassword { get; init; }
    public bool IsSuperAdmin { get; init; }
}
