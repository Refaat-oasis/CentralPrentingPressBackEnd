using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "رمز التحديث مطلوب")]
    public string RefreshToken { get; init; } = string.Empty;
}
