namespace CentralPress.API.ViewModels;

public class ModulePermissionViewModel
{
    public int ModuleId { get; init; }
    public string ModuleKey { get; init; } = string.Empty;
    public string ModuleNameAr { get; init; } = string.Empty;
    public bool CanAccess { get; init; }
    public bool CanCreate { get; init; }
    public bool CanUpdate { get; init; }
    public bool CanToggleActive { get; init; }
}
