using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class PaperCuttingCalculationRequestDto
{
    [Range(10.0, 5000.0, ErrorMessage = "عرض فرخ الورق يجب أن يكون بين 10 و 5000 مم")]
    public decimal ParentSheetWidthMm { get; set; }

    [Range(10.0, 5000.0, ErrorMessage = "طول فرخ الورق يجب أن يكون بين 10 و 5000 مم")]
    public decimal ParentSheetHeightMm { get; set; }

    [Range(5.0, 5000.0, ErrorMessage = "عرض المنتج المقصوص يجب أن يكون بين 5 و 5000 مم")]
    public decimal CutItemWidthMm { get; set; }

    [Range(5.0, 5000.0, ErrorMessage = "طول المنتج المقصوص يجب أن يكون بين 5 و 5000 مم")]
    public decimal CutItemHeightMm { get; set; }

    [Range(1, 10000000, ErrorMessage = "عدد النسخ المطلوبة يجب أن يكون على الأقل 1")]
    public int RequiredItemCopies { get; set; }

    [Range(0.0, 50.0, ErrorMessage = "هامش التهديب يجب أن يكون بين 0 و 50 مم")]
    public decimal TrimmingMarginMm { get; set; } = 5.0m;

    [Range(0, 10000, ErrorMessage = "تالف ضبط الماكينة يجب ألا يقل عن 0")]
    public int SetupSpoilageSheets { get; set; } = 50;

    [Range(0.0, 100.0, ErrorMessage = "نسبة تالف التشغيل يجب أن تكون بين 0 و 100%")]
    public decimal RunSpoilagePercent { get; set; } = 3.0m;
}
