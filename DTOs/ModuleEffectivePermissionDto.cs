namespace CentralPress.API.DTOs;

public record ModuleEffectivePermissionDto(
    string ModuleKey,
    string ModuleNameAr,
    bool CanAccess,
    bool CanCreate,
    bool CanUpdate,
    bool CanToggleActive
);
