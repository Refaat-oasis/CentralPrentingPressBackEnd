using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CentralPress.API.Entities;

[Table("MoqaysaAccountShares")]
public class MoqaysaAccountShare
{
    [Key]
    public int ShareId { get; set; }
    public int MoqaysaId { get; set; }
    [ForeignKey(nameof(MoqaysaId))]
    public virtual Moqaysa? Moqaysa { get; set; }
    public int AccountId { get; set; }
    [ForeignKey(nameof(AccountId))]
    public virtual AccountEntity? Account { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal SharePercentage { get; set; } = 100.00m;
    [Column(TypeName = "decimal(18,2)")]
    public decimal AllocatedAmount { get; set; }

    public bool IsIncludedInTasweya { get; set; } = false;
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAtUtc { get; set; }
}
