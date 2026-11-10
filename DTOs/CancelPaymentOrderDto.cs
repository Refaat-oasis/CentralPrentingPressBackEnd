using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class CancelPaymentOrderDto
{
    [Required(ErrorMessage = "يرجى ذكر سبب إلغاء أمر الدفع")]
    [MaxLength(1000, ErrorMessage = "سبب الإلغاء يجب ألا يتجاوز 1000 حرف")]
    public string Reason { get; set; } = string.Empty;
}
