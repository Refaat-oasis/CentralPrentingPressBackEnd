using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يتكون من 6 أرقام")]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور يجب أن لا تقل عن 8 خانات")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "كلمة المرور يجب أن تتضمن 8 خانات على الأقل، وتحتوي على حرف كبير وحرف صغير ورقم واحد على الأقل")]
    public string NewPassword { get; init; } = string.Empty;
}
