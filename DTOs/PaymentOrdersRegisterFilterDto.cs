namespace CentralPress.API.DTOs;

public class PaymentOrdersRegisterFilterDto
{
    public int? AccountId { get; set; }
    public bool? IsConfirmed { get; set; }
}
