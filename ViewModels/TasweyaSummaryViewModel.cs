using System;

namespace CentralPress.API.ViewModels;

public class TasweyaSummaryViewModel
{
    public int TasweyaId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateOnly SettlementDate { get; set; }
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerNameAr { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string JobOrderNumbersText { get; set; } = string.Empty;
    public decimal SettlementGrandTotal { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public bool IsActive { get; set; }
    public int? ActivePaymentOrderId { get; set; }
    public string? ActivePaymentOrderNumber { get; set; }
    public bool HasActivePaymentOrder => ActivePaymentOrderId.HasValue;
    public DateTime CreatedAtUtc { get; set; }
}
