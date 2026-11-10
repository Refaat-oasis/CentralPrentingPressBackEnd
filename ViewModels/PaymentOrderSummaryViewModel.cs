using System;
using CentralPress.API.Models;

namespace CentralPress.API.ViewModels;

public class PaymentOrderSummaryViewModel
{
    public int PaymentOrderId { get; set; }
    public string PaymentOrderNumber { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerNameAr { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
    public PaymentOrderStatus Status { get; set; }
    public string StatusTextAr { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? ConfirmedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int LinkedTasweyatCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
