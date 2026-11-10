using System;
using System.Collections.Generic;
using CentralPress.API.Models;

namespace CentralPress.API.ViewModels;

public class PaymentOrderDetailViewModel
{
    public int PaymentOrderId { get; set; }
    public string PaymentOrderNumber { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public int AccountId { get; set; }
    public AccountViewModel? Account { get; set; }
    public int? CustomerId { get; set; }
    public CustomerViewModel? Customer { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;

    public PaymentOrderStatus Status { get; set; }
    public string StatusTextAr { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }

    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public string? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }

    public string? PaidByUserId { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public string? RejectedByUserId { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }

    public string? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public decimal? CustomerBudgetCeiling { get; set; }
    public decimal? RemainingBudget { get; set; }

    public List<PaymentOrderTasweyaLinkViewModel> TasweyaLinks { get; set; } = new();
}
