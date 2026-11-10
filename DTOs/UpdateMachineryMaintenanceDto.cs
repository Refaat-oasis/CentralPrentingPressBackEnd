using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class UpdateMachineryMaintenanceDto
{
    [Required(ErrorMessage = "يرجى تحديد حالة الصيانة")]
    [MaxLength(50, ErrorMessage = "حالة الصيانة يجب ألا تتجاوز 50 حرف")]
    public string MaintenanceStatus { get; set; } = "Operational";

    [MaxLength(500, ErrorMessage = "ملاحظات الصيانة يجب ألا تتجاوز 500 حرف")]
    public string? MaintenanceNotes { get; set; }
}
