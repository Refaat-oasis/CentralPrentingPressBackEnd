using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MaterialDto
{
    public int? MaterialId { get; set; }

    [Required(ErrorMessage = "يرجى كتابة اسم الخامة")]
    [MaxLength(150, ErrorMessage = "اسم الخامة يجب ألا يتجاوز 150 حرف")]
    public string MaterialNameAr { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "وحدة القياس يجب ألا تتجاوز 50 حرف")]
    public string? UnitNameAr { get; set; }

    [Range(0.0, 1000000.0, ErrorMessage = "السعر القياسي للوحدة يجب ألا يكون سالباً")]
    public decimal StandardUnitPrice { get; set; } = 0.00m;

    [MaxLength(50, ErrorMessage = "تصنيف الخامة يجب ألا يتجاوز 50 حرف")]
    public string? CategoryAr { get; set; }

    public bool IsActive { get; set; } = true;
}
