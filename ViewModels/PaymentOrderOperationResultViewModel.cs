using CentralPress.API.Models;

namespace CentralPress.API.ViewModels;

public class PaymentOrderOperationResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? PaymentOrderId { get; set; }
    public PaymentOrderStatus? Status { get; set; }
    public decimal? TotalPaidAmount { get; set; }
    public decimal? RemainingBudget { get; set; }
}
