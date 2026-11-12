using System;
using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class MoqaysaDetailViewModel
{
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? AccountCode { get; set; }
    public CustomerViewModel? Customer { get; set; }
    public string RequiredQuantityText { get; set; } = string.Empty;
    public string SpecificationsText { get; set; } = string.Empty;

    public decimal RawMaterials { get; set; }
    public decimal MaterialProcessingPercent { get; set; } = 5.5m;
    public decimal MaterialProcessingFee { get; set; }
    public decimal FilmsProcessingFee { get; set; }
    public decimal TotalMaterials { get; set; }

    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal SubTotal1 { get; set; }

    public bool ApplyAdminExpenses { get; set; }
    public decimal AdminExpensesPercent { get; set; } = 8.5m;
    public decimal AdminExpenses { get; set; }
    public decimal TotalExpenses { get; set; }

    public bool ApplyMarkup { get; set; }
    public decimal SelectedMarkupPercent { get; set; } = 8.5m;
    public decimal MarkupAmount { get; set; }
    public decimal TotalAfterMarkup { get; set; }

    public decimal MinistryOfFinancePercent { get; set; } = 15.0m;
    public decimal MinistryOfFinanceFee { get; set; }
    public decimal WorkersFundPercent { get; set; } = 10.0m;
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials";
    public decimal WorkersSyndicateFund { get; set; }

    public decimal SubTotal2 { get; set; }
    public bool ApplyOther { get; set; }
    public decimal OtherPercent { get; set; } = 10.0m;
    public decimal OtherAmount { get; set; }
    public decimal SubTotal3 { get; set; }

    public bool ApplyVat { get; set; }
    public decimal VatPercent { get; set; } = 14.0m;
    public decimal VatAmount { get; set; }
    public decimal FinalGrandTotal { get; set; }

    public decimal ParsedQuantity { get; set; }
    public decimal UnitPriceBeforeVat { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public string? TafqeetArabicText { get; set; }

    public bool IsIncludedInTasweya { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
    public bool AreMachineryHoursLinked { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public List<MoqaysaMaterialItemViewModel> MaterialItems { get; set; } = new();
    public List<MoqaysaMachineryItemViewModel> MachineryItems { get; set; } = new();
    public List<MoqaysaAccountShareViewModel> AccountShares { get; set; } = new();
}
