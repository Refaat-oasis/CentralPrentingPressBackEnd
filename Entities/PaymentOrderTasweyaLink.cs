using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class PaymentOrderTasweyaLink
{
    [Key]
    public int PaymentLinkId { get; set; }

    public int PaymentOrderId { get; set; }

    [ForeignKey(nameof(PaymentOrderId))]
    public virtual PaymentOrder? PaymentOrder { get; set; }

    public int TasweyaId { get; set; }

    [ForeignKey(nameof(TasweyaId))]
    public virtual Tasweya? Tasweya { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SnapshotTasweyaAmount { get; set; }

    public bool IsActive { get; set; } = true;
}
