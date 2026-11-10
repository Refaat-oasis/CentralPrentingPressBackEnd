namespace CentralPress.API.Services;

public class MoqaysaCalculationInput
{
    public decimal RawMaterials { get; set; }
    public decimal FilmsProcessingFee { get; set; } = 0.00m;
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }

    public bool ApplyAdminExpenses { get; set; }
    public bool ApplyMarkup { get; set; }
    public decimal SelectedMarkupPercent { get; set; } = 8.5m;
    public bool ApplyOther { get; set; }
    public bool ApplyVat { get; set; }

    public decimal MaterialProcessingPercent { get; set; } = 5.5m;
    public decimal AdminExpensesPercent { get; set; } = 8.5m;
    public decimal MinistryOfFinancePercent { get; set; } = 15.0m;
    public decimal WorkersFundPercent { get; set; } = 10.0m;
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials";
    public decimal OtherPercent { get; set; } = 10.0m;
    public decimal VatPercent { get; set; } = 14.0m;

    public decimal Quantity { get; set; } = 1.0m;
}
