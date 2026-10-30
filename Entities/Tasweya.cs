using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

[Table("Tasweyat")]
public class Tasweya
{
    [Key]
    public int TasweyaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SettlementNumber { get; set; } = string.Empty;

    public DateOnly SettlementDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public virtual AccountEntity? Account { get; set; }

    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [MaxLength(500)]
    public string JobOrderNumbersText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ClaimNumber { get; set; } = string.Empty;

    // Aggregated Breakdown Items for the Leftmost 'أفرادى' column
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMaterialsSum { get; set; } // خامات

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWorkerWagesSum { get; set; } // أجور عمال

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMachineCostsSum { get; set; } // م . الآلات

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdminExpensesSum { get; set; } // م . إدارية

    [Column(TypeName = "decimal(18,2)")]
    public decimal MarkupAmountSum { get; set; } // النسبة المضافة

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinistryOfFinanceSum { get; set; } // وزارة المالية

    [Column(TypeName = "decimal(18,2)")]
    public decimal WorkersFundSum { get; set; } // ص. ت. العاملين

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherAmountSum { get; set; } // أخرى

    // Left 'اجمالى' totals
    [Column(TypeName = "decimal(18,2)")]
    public decimal RevenuesTotal { get; set; } // حـ / الإيرادات

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatTotal { get; set; } // حـ / القيمة المضافة

    // Master Balanced Grand Total (Placed in Right 'اجمالى' and Left bottom 'اجمالى')
    [Column(TypeName = "decimal(18,2)")]
    public decimal SettlementGrandTotal { get; set; }

    [MaxLength(1000)]
    public string TotalAmountInWordsAr { get; set; } = string.Empty;

    // Payment Tracking
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<TasweyaMoqaysaLink> MoqaysaLinks { get; set; } = new List<TasweyaMoqaysaLink>();
    public virtual ICollection<PaymentOrderTasweyaLink> PaymentOrderLinks { get; set; } = new List<PaymentOrderTasweyaLink>();
}
