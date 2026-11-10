using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class RolePermissionsMatrixViewModel
{
    public int JobTitleId { get; init; }
    public string TitleNameAr { get; init; } = string.Empty;
    public List<ModulePermissionViewModel> Permissions { get; init; } = new();
}
