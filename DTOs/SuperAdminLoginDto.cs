namespace CentralPress.API.DTOs;

public class SuperAdminLoginDto
{
    private string _code = string.Empty;

    public string Code
    {
        get => !string.IsNullOrWhiteSpace(_code) ? _code : (TotpToken ?? string.Empty);
        init => _code = value;
    }

    public string? TotpToken { get; init; }

    public string? SecretCode { get; init; }
}
