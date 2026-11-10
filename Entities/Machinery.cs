using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class Machinery
{
    [Key]
    public int MachineryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string MachineryNameAr { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal WorkerHourlyFee { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MachineOperatingHourlyFee { get; set; }

    [MaxLength(50)]
    public string MaintenanceStatus { get; set; } = "Operational";

    [MaxLength(500)]
    public string? MaintenanceNotes { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DepreciationHourlyRate { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaintenanceHourlyRate { get; set; } = 0.00m;

    public bool IsActive { get; set; } = true;
}
