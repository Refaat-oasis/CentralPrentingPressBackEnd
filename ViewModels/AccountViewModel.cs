namespace CentralPress.API.ViewModels;

public class AccountViewModel
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public string? DebitHeaderTextAr { get; set; }
    public decimal? BudgetCeiling { get; set; }
    public bool IsActive { get; set; }
}
