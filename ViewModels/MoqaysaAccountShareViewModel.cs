using System;

namespace CentralPress.API.ViewModels;
public class MoqaysaAccountShareViewModel
{
    public int ShareId { get; set; }
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public string DebitHeaderTextAr { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; }
    public decimal AllocatedAmount { get; set; }
    public bool IsIncludedInTasweya { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public int? TasweyaId { get; set; }
    public string? SettlementNumber { get; set; }
}
