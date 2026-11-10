using System;

namespace CentralPress.API.ViewModels;

public class AuthTokenViewModel
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
