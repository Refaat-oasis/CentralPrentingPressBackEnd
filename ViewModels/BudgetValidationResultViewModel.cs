namespace CentralPress.API.ViewModels;

public class BudgetValidationResultViewModel
{
    public bool IsValid { get; set; }
    public decimal? BudgetCeiling { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal RemainingBudget { get; set; }
    public string Message { get; set; } = string.Empty;
}
