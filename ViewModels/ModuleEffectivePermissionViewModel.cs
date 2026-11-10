namespace CentralPress.API.ViewModels;

public class ModuleEffectivePermissionViewModel
{
    public string ModuleKey { get; init; } = string.Empty;
    public string ModuleNameAr { get; init; } = string.Empty;
    public bool CanAccess { get; init; }
    public bool CanCreate { get; init; }
    public bool CanUpdate { get; init; }
    public bool CanToggleActive { get; init; }

    public ModuleEffectivePermissionViewModel() { }

    public ModuleEffectivePermissionViewModel(
        string moduleKey,
        string moduleNameAr,
        bool canAccess,
        bool canCreate,
        bool canUpdate,
        bool canToggleActive)
    {
        ModuleKey = moduleKey;
        ModuleNameAr = moduleNameAr;
        CanAccess = canAccess;
        CanCreate = canCreate;
        CanUpdate = canUpdate;
        CanToggleActive = canToggleActive;
    }
}
