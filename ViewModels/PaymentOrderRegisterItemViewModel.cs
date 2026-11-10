using System;
using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class PaymentOrderRegisterItemViewModel
{
    public int PaymentOrderId { get; set; }
    public string PaymentOrderNumber { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public string CustomerNameAr { get; set; } = string.Empty;
    public decimal TotalPaidAmount { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? Notes { get; set; }
    public List<PaymentOrderTasweyaLinkViewModel> Tasweyat { get; set; } = new();
}
