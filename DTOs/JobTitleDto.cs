using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class JobTitleDto
{
    [Required(ErrorMessage = "اسم المسمى الوظيفي مطلوب")]
    [MaxLength(150)]
    public string TitleNameAr { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }
}
