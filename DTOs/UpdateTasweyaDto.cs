using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class UpdateTasweyaDto
{
    [Required(ErrorMessage = "يرجى كتابة رقم كشف التسوية")]
    [MaxLength(100)]
    public string SettlementNumber { get; set; } = string.Empty;

    public DateOnly SettlementDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Range(1, int.MaxValue, ErrorMessage = "الحساب المختار غير صحيح")]
    public int AccountId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "الجهة الطالبة غير موجودة")]
    public int CustomerId { get; set; }

    [MaxLength(100)]
    public string? ClaimNumber { get; set; }

    [Required(ErrorMessage = "يرجى تحديد مقايسة واحدة على الأقل")]
    [MinLength(1, ErrorMessage = "يرجى تحديد مقايسة واحدة على الأقل")]
    public List<int> MoqaysaIds { get; set; } = new();

    [Range(0, 100, ErrorMessage = "نسبة الإضافة يجب أن تكون بين 0 و 100")]
    public decimal? OverrideMarkupPercent { get; set; }
}
