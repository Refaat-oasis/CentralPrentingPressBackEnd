using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; init; } = string.Empty;
}
