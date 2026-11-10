using System.Collections.Generic;

namespace CentralPress.API.Common;

public static class PermissionActions
{
    public const string Access = "access";
    public const string Create = "create";
    public const string Update = "update";
    public const string ToggleActive = "toggleactive";

    public static readonly IReadOnlyList<string> All =
    [
        Access,
        Create,
        Update,
        ToggleActive
    ];
}
