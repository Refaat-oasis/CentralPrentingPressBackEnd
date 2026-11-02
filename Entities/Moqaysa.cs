using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class Moqaysa
{
    [Key]
    public int MoqaysaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JobOrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [MaxLength(500)]
    public string RequiredQuantityText { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string SpecificationsText { get; set; } = string.Empty;

    // Financial Cascade (Points 26 - 41)
    [Column(TypeName = "decimal(18,2)")]
    public decimal RawMaterials { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedMaterialProcessingPercent { get; set; } = 5.5m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialProcessingFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FilmsProcessingFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMaterials { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWorkerWages { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMachineCosts { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal1 { get; set; }

    public bool ApplyAdminExpenses { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedAdminExpensesPercent { get; set; } = 8.5m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdminExpenses { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalExpenses { get; set; }

    public bool ApplyMarkup { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SelectedMarkupPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MarkupAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAfterMarkup { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedMinistryOfFinancePercent { get; set; } = 15.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinistryOfFinanceFee { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedWorkersFundPercent { get; set; } = 10.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal WorkersSyndicateFund { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal2 { get; set; }

    public bool ApplyOther { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedOtherPercent { get; set; } = 10.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal3 { get; set; }

    public bool ApplyVat { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SelectedVatPercent { get; set; } = 14.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalGrandTotal { get; set; }

    // State Tracking
    public bool IsIncludedInTasweya { get; set; } = false;
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<MoqaysaMaterialItem> MaterialItems { get; set; } = new List<MoqaysaMaterialItem>();
    public virtual ICollection<MoqaysaMachineryItem> MachineryItems { get; set; } = new List<MoqaysaMachineryItem>();
    public virtual ICollection<TasweyaMoqaysaLink> TasweyaLinks { get; set; } = new List<TasweyaMoqaysaLink>();
}
