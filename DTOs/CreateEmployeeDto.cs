using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "كود الموظف مطلوب")]
    [MaxLength(50)]
    public string EmployeeCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [MaxLength(250)]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    public string Email { get; init; } = string.Empty;

    public string? DummyPassword { get; init; }

    public int? JobTitleId { get; init; }

    [MaxLength(500)]
    public string? PhotoUrl { get; init; }
}
