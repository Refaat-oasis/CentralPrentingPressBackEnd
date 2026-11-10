using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MoqaysaSaveDto
{
    [Required(ErrorMessage = "يرجى إدخال رقم إذن التشغيل")]
    [MaxLength(100, ErrorMessage = "رقم إذن التشغيل يجب ألا يتجاوز 100 حرف")]
    public string JobOrderNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار الجهة الطالبة")]
    public int CustomerId { get; set; }

    [MaxLength(500, ErrorMessage = "وصف الكمية يجب ألا يتجاوز 500 حرف")]
    public string RequiredQuantityText { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "المواصفات يجب ألا تتجاوز 2000 حرف")]
    public string SpecificationsText { get; set; } = string.Empty;

    [Range(0.0, 1000000.0, ErrorMessage = "تكلفة تشغيل الأفلام يجب ألا تكون سالبة")]
    public decimal FilmsProcessingFee { get; set; } = 0.00m;

    public bool ApplyAdminExpenses { get; set; }

    public bool ApplyMarkup { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة الأرباح يجب أن تكون بين 0 و 100")]
    public decimal SelectedMarkupPercent { get; set; } = 8.5m;

    public bool ApplyOther { get; set; }

    public bool ApplyVat { get; set; }

    public bool? AreMachineryHoursLinked { get; set; } = true;

    [Range(0.0, 100.0, ErrorMessage = "نسبة تشغيل الخامات يجب أن تكون بين 0 و 100")]
    public decimal? SelectedMaterialProcessingPercent { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة المصروفات الإدارية يجب أن تكون بين 0 و 100")]
    public decimal? SelectedAdminExpensesPercent { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة وزارة المالية يجب أن تكون بين 0 و 100")]
    public decimal? SelectedMinistryOfFinancePercent { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة صندوق العاملين يجب أن تكون بين 0 و 100")]
    public decimal? SelectedWorkersFundPercent { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة المصروفات الأخرى يجب أن تكون بين 0 و 100")]
    public decimal? SelectedOtherPercent { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة ضريبة القيمة المضافة يجب أن تكون بين 0 و 100")]
    public decimal? SelectedVatPercent { get; set; }

    public List<MaterialItemInputDto> MaterialItems { get; set; } = new();

    public List<MachineryItemInputDto> MachineryItems { get; set; } = new();
}
