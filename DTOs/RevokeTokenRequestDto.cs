using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class RevokeTokenRequestDto
{
    [Required(ErrorMessage = "رمز التحديث مطلوب")]
    public string RefreshToken { get; init; } = string.Empty;
}
