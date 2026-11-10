namespace CentralPress.API.ViewModels;

public class UserPermissionOverrideViewModel
{
    public int OverrideId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public int ModuleId { get; init; }
    public bool? CanAccess { get; init; }
    public bool? CanCreate { get; init; }
    public bool? CanUpdate { get; init; }
    public bool? CanToggleActive { get; init; }
}
