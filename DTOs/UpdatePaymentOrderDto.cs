using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class UpdatePaymentOrderDto
{
    [Required(ErrorMessage = "يرجى إدخال رقم أمر الدفع الرسمي")]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z0-9_\-\.]{3,50}$", ErrorMessage = "رقم أمر الدفع غير صالح")]
    public string PaymentOrderNumber { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int? AccountId { get; set; }

    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "يرجى تحديد كشف تسوية واحد على الأقل")]
    [MinLength(1, ErrorMessage = "يرجى تحديد كشف تسوية واحد على الأقل")]
    public List<int> TasweyaIds { get; set; } = new();

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
