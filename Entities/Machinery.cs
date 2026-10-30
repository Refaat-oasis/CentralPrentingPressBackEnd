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

    [Column(TypeName = "decimal(18,2)")]
    public decimal WorkerHourlyFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MachineOperatingHourlyFee { get; set; }

    public bool IsActive { get; set; } = true;
}
