namespace CentralPress.API.Common;

public static class PermissionConstants
{
    public const string ClaimType = "permission";

    public static string CreatePermission(string module, string action) =>
        $"{module.ToLowerInvariant()}:{action.ToLowerInvariant()}";

    public static string LegacyPolicyName(string module, string action) =>
        $"{module.ToUpperInvariant()}:Can{char.ToUpper(action[0])}{action[1..]}";
}
