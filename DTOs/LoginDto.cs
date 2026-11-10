using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "اسم المستخدم أو كود الموظف مطلوب")]
    public string Identifier { get; init; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password { get; init; } = string.Empty;
}
