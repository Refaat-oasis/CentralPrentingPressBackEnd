using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class PaymentOrder
{
    [Key]
    public int PaymentOrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PaymentOrderNumber { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public virtual AccountEntity? Account { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPaidAmount { get; set; }

    [MaxLength(1000)]
    public string TotalAmountInWordsAr { get; set; } = string.Empty;

    public bool IsConfirmed { get; set; } = false;

    public DateTime? ConfirmedAtUtc { get; set; }

    [MaxLength(450)]
    public string? ConfirmedByUserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<PaymentOrderTasweyaLink> TasweyaLinks { get; set; } = new List<PaymentOrderTasweyaLink>();
}
