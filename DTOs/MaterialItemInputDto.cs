using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MaterialItemInputDto
{
    public int? MaterialId { get; set; }

    [MaxLength(200)]
    public string? MaterialNameSnapshot { get; set; }

    [Range(0.001, 10000000.0, ErrorMessage = "الكمية يجب أن تكون قيمة موجبة أكبر من الصفر")]
    public decimal Quantity { get; set; }

    [Range(0.0001, 1000000.0, ErrorMessage = "سعر الوحدة يجب أن يكون قيمة موجبة أكبر من الصفر")]
    public decimal UnitPrice { get; set; }
}
