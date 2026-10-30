using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class MoqaysaMaterialItem
{
    [Key]
    public int ItemId { get; set; }

    public int MoqaysaId { get; set; }

    [ForeignKey(nameof(MoqaysaId))]
    public virtual Moqaysa? Moqaysa { get; set; }

    public int? MaterialId { get; set; }

    [ForeignKey(nameof(MaterialId))]
    public virtual Material? Material { get; set; }

    [Required]
    [MaxLength(200)]
    public string MaterialNameSnapshot { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
