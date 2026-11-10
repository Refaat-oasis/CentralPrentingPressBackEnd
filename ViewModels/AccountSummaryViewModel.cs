namespace CentralPress.API.ViewModels;

public class AccountSummaryViewModel
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public string DebitHeaderTextAr { get; set; } = string.Empty;
    public decimal TotalBilledAmount { get; set; }
    public decimal TotalClaimedInTasweya { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalUnpaidAmount { get; set; }
    public decimal UnpaidClaimsInTasweya { get; set; }
    public decimal UnsettledMoqaysat { get; set; }
    public int MoqaysatCount { get; set; }
    public int TasweyatCount { get; set; }
    public int PaidTasweyatCount { get; set; }
}
