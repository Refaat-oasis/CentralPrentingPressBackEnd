using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class VerifyResetCodeDto
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يتكون من 6 أرقام")]
    public string Code { get; init; } = string.Empty;
}
