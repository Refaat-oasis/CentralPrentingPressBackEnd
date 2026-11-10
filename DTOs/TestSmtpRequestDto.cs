using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class TestSmtpRequestDto
{
    [Required(ErrorMessage = "البريد الإلكتروني التجريبي مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string RecipientEmail { get; init; } = string.Empty;
}
