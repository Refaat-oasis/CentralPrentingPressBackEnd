using System;

namespace CentralPress.API.ViewModels;

public class PaymentOrderTasweyaLinkViewModel
{
    public int PaymentLinkId { get; set; }
    public int PaymentOrderId { get; set; }
    public int TasweyaId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateOnly SettlementDate { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string CustomerNameAr { get; set; } = string.Empty;
    public string JobOrderNumbersText { get; set; } = string.Empty;
    public decimal SnapshotTasweyaAmount { get; set; }
    public bool IsActive { get; set; }
}
