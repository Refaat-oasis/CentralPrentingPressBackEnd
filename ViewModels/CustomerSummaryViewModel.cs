using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class CustomerSummaryViewModel
{
    public int CustomerId { get; set; }
    public string CustomerNameAr { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal TotalBilledAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalUnpaidAmount { get; set; }
    public int MoqaysatCount { get; set; }
    public int TasweyatCount { get; set; }
    public int PaidTasweyatCount { get; set; }
    public List<CustomerMoqaysaSummaryViewModel> Moqaysat { get; set; } = new();
    public List<CustomerTasweyaSummaryViewModel> Tasweyat { get; set; } = new();
}
