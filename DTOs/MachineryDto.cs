using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MachineryDto
{
    public int? MachineryId { get; set; }

    [Required(ErrorMessage = "يرجى كتابة اسم الآلة / العملية")]
    [MaxLength(150, ErrorMessage = "اسم الآلة يجب ألا يتجاوز 150 حرف")]
    public string MachineryNameAr { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "أجر العامل بالساعة يجب ألا يكون سالباً")]
    public decimal WorkerHourlyFee { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "تكلفة تشغيل الآلة بالساعة يجب ألا تكون سالبة")]
    public decimal MachineOperatingHourlyFee { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "معدل إهلاك الآلة بالساعة يجب ألا يكون سالباً")]
    public decimal DepreciationHourlyRate { get; set; } = 0.00m;

    [Range(0.0, 100000.0, ErrorMessage = "معدل صيانة الآلة بالساعة يجب ألا يكون سالباً")]
    public decimal MaintenanceHourlyRate { get; set; } = 0.00m;

    [MaxLength(50, ErrorMessage = "حالة الصيانة يجب ألا تتجاوز 50 حرف")]
    public string? MaintenanceStatus { get; set; } = "Operational";

    [MaxLength(500, ErrorMessage = "ملاحظات الصيانة يجب ألا تتجاوز 500 حرف")]
    public string? MaintenanceNotes { get; set; }

    public bool IsActive { get; set; } = true;
}
