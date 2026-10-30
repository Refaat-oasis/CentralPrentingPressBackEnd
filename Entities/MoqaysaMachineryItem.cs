using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class MoqaysaMachineryItem
{
    [Key]
    public int OperationId { get; set; }

    public int MoqaysaId { get; set; }

    [ForeignKey(nameof(MoqaysaId))]
    public virtual Moqaysa? Moqaysa { get; set; }

    public int MachineryId { get; set; }

    [ForeignKey(nameof(MachineryId))]
    public virtual Machinery? Machinery { get; set; }

    [Required]
    [MaxLength(150)]
    public string MachineryNameSnapshot { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Hours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WorkerHourlyRateSnapshot { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWorkerWages { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MachineHourlyRateSnapshot { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMachineOperatingCost { get; set; }
}
