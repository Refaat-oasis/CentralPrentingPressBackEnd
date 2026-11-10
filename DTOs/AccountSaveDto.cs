using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class AccountSaveDto
{
    public int? AccountId { get; set; }

    [RegularExpression(@"^(ACC-\d{1,8}|[A-Z0-9_\-]{3,20}|\d{4,12})$", ErrorMessage = "كود الحساب/الموازنة غير صالح")]
    [MaxLength(50, ErrorMessage = "كود الحساب يجب ألا يتجاوز 50 حرف")]
    public string? AccountCode { get; set; }

    [Required(ErrorMessage = "يرجى إدخال اسم الحساب المحاسبي")]
    [MaxLength(200, ErrorMessage = "اسم الحساب يجب ألا يتجاوز 200 حرف")]
    public string AccountNameAr { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "نص ترويسة المدين يجب ألا يتجاوز 200 حرف")]
    public string? DebitHeaderTextAr { get; set; }

    [Range(0, 100_000_000_000, ErrorMessage = "سقف الميزانية يجب أن يكون قيمة موجبة")]
    public decimal? BudgetCeiling { get; set; }

    public bool IsActive { get; set; } = true;
}
