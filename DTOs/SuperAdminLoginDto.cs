using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class SuperAdminLoginDto
{
    private string _code = string.Empty;

    [MaxLength(64)]
    public string Code
    {
        get => !string.IsNullOrWhiteSpace(_code) ? _code : (TotpToken ?? string.Empty);
        init => _code = value;
    }

    [MaxLength(64)]
    public string? TotpToken { get; init; }

    [MaxLength(128)]
    public string? SecretCode { get; init; }
}
