using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class LoginResponseViewModel
{
    public string Token { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public UserProfileViewModel User { get; init; } = null!;
    public IEnumerable<object>? Permissions { get; init; }
}
