using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    [MaxLength(250)]
    public string FullName { get; init; } = string.Empty;
}
