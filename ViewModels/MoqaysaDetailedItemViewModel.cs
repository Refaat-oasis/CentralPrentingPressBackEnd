using System;

namespace CentralPress.API.ViewModels;

public class MoqaysaDetailedItemViewModel
{
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public string CustomerNameAr { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal RawMaterials { get; set; }
    public decimal MaterialProcessingFee { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal AdminExpenses { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal MinistryOfFinanceFee { get; set; }
    public decimal WorkersSyndicateFund { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalGrandTotal { get; set; }
    public bool IsIncludedInTasweya { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
