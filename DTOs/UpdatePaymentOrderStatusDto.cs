using System.ComponentModel.DataAnnotations;
using CentralPress.API.Models;

namespace CentralPress.API.DTOs;

public class UpdatePaymentOrderStatusDto
{
    public PaymentOrderStatus? NewStatus { get; set; }

    public PaymentOrderStatus? Status
    {
        get => NewStatus;
        set => NewStatus = value;
    }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
