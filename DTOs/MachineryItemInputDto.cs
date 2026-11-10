using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MachineryItemInputDto
{
    public int MachineryId { get; set; }

    [MaxLength(200)]
    public string? MachineryNameSnapshot { get; set; }

    [Range(0.0, 10000.0, ErrorMessage = "عدد الساعات يجب ألا يكون سالباً")]
    public decimal Hours { get; set; }

    [Range(0.0, 10000.0, ErrorMessage = "ساعات تشغيل الآلة يجب ألا تكون سالبة")]
    public decimal? MachineHours { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "أجر العامل بالساعة يجب ألا يكون سالباً")]
    public decimal WorkerHourlyRateSnapshot { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "تكلفة تشغيل الآلة بالساعة يجب ألا تكون سالبة")]
    public decimal MachineHourlyRateSnapshot { get; set; }
}
