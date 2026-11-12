using System;
using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class TasweyaUnsettledMoqaysaViewModel
{
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerNameAr { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public string RequiredQuantityText { get; set; } = string.Empty;

    public decimal RawMaterials { get; set; }
    public decimal MaterialProcessingFee { get; set; }
    public decimal FilmsProcessingFee { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal AdminExpenses { get; set; }
    public decimal MarkupAmount { get; set; }
    public bool ApplyMarkup { get; set; }
    public decimal SelectedMarkupPercent { get; set; }
    public decimal MinistryOfFinanceFee { get; set; }
    public decimal WorkersSyndicateFund { get; set; }
    public decimal OtherAmount { get; set; }
    public bool ApplyVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalGrandTotal { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<MoqaysaAccountShareViewModel> AccountShares { get; set; } = new();
}
