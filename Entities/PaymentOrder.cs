using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CentralPress.API.Models;

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

    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Draft;

    public bool IsConfirmed { get; set; } = false;

    public DateTime? ConfirmedAtUtc { get; set; }

    [MaxLength(450)]
    public string? ConfirmedByUserId { get; set; }

    [MaxLength(450)]
    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    [MaxLength(450)]
    public string? PaidByUserId { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    [MaxLength(450)]
    public string? RejectedByUserId { get; set; }

    public DateTime? RejectedAtUtc { get; set; }

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    [MaxLength(450)]
    public string? CancelledByUserId { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<PaymentOrderTasweyaLink> TasweyaLinks { get; set; } = new List<PaymentOrderTasweyaLink>();
}
