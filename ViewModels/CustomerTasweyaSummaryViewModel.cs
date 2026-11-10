using System;

namespace CentralPress.API.ViewModels;

public class CustomerTasweyaSummaryViewModel
{
    public int TasweyaId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public decimal SettlementGrandTotal { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateOnly SettlementDate { get; set; }
}
