namespace CentralPress.API.ViewModels;

public class MoqaysaCalculationResultViewModel
{
    public decimal RawMaterials { get; set; }
    public decimal MaterialProcessingPercent { get; set; }
    public decimal MaterialProcessingFee { get; set; }
    public decimal FilmsProcessingFee { get; set; }
    public decimal TotalMaterials { get; set; }

    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal SubTotal1 { get; set; }

    public bool ApplyAdminExpenses { get; set; }
    public decimal AdminExpensesPercent { get; set; }
    public decimal AdminExpenses { get; set; }
    public decimal TotalExpenses { get; set; }

    public bool ApplyMarkup { get; set; }
    public decimal SelectedMarkupPercent { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal TotalAfterMarkup { get; set; }

    public decimal MinistryOfFinancePercent { get; set; }
    public decimal MinistryOfFinanceFee { get; set; }
    public decimal WorkersFundPercent { get; set; }
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials";
    public decimal WorkersSyndicateFund { get; set; }

    public decimal SubTotal2 { get; set; }
    public bool ApplyOther { get; set; }
    public decimal OtherPercent { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal SubTotal3 { get; set; }

    public bool ApplyVat { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalGrandTotal { get; set; }

    public decimal ParsedQuantity { get; set; }
    public decimal UnitPriceBeforeVat { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public string? TafqeetArabicText { get; set; }
}
