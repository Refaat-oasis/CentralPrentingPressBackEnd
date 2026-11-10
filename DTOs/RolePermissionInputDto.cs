namespace CentralPress.API.DTOs;

public class RolePermissionInputDto
{
    public int ModuleId { get; init; }
    public bool CanAccess { get; init; }
    public bool CanCreate { get; init; }
    public bool CanUpdate { get; init; }
    public bool CanToggleActive { get; init; }
}
