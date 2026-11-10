using System;

namespace CentralPress.API.ViewModels;

public class MoqaysaSummaryViewModel
{
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? AccountCode { get; set; }
    public string RequiredQuantityText { get; set; } = string.Empty;
    public decimal RawMaterials { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal SubTotal1 { get; set; }
    public decimal SubTotal2 { get; set; }
    public decimal SubTotal3 { get; set; }
    public decimal FinalGrandTotal { get; set; }
    public bool ApplyVat { get; set; }
    public decimal VatAmount { get; set; }
    public bool IsIncludedInTasweya { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
