namespace CentralPress.API.ViewModels;

public class CustomerViewModel
{
    public int CustomerId { get; set; }
    public string CustomerNameAr { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountNameAr { get; set; }
    public string? DebitHeaderTextAr { get; set; }
    public decimal? BudgetCeiling { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
