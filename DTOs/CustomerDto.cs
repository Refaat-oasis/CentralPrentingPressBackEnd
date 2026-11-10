using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class CustomerDto
{
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "يرجى كتابة اسم الجهة الطالبة / العميل")]
    [MaxLength(250, ErrorMessage = "اسم العميل يجب ألا يتجاوز 250 حرف")]
    public string CustomerNameAr { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "الحساب المختار غير صحيح")]
    public int AccountId { get; set; }

    [Range(0, 100_000_000_000, ErrorMessage = "سقف الميزانية يجب أن يكون قيمة موجبة")]
    public decimal? BudgetCeiling { get; set; }

    [MaxLength(50, ErrorMessage = "رقم الهاتف يجب ألا يتجاوز 50 حرف")]
    public string? Phone { get; set; }

    [MaxLength(150, ErrorMessage = "البريد الإلكتروني يجب ألا يتجاوز 150 حرف")]
    public string? Email { get; set; }

    [MaxLength(300, ErrorMessage = "العنوان يجب ألا يتجاوز 300 حرف")]
    public string? Address { get; set; }

    [MaxLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
