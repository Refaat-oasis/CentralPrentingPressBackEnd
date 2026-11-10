using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MoqaysaCalculationRequestDto
{
    [Range(0.0, 10000000.0, ErrorMessage = "كمية الإنتاج يجب ألا تكون سالبة")]
    public decimal Quantity { get; set; } = 1.0m;

    [MaxLength(500)]
    public string? RequiredQuantityText { get; set; }

    [Range(0.0, 1000000.0, ErrorMessage = "تكلفة تشغيل الأفلام يجب ألا تكون سالبة")]
    public decimal FilmsProcessingFee { get; set; } = 0.00m;

    public bool ApplyAdminExpenses { get; set; }

    public bool ApplyMarkup { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "نسبة الأرباح يجب أن تكون بين 0 و 100")]
    public decimal SelectedMarkupPercent { get; set; } = 8.5m;

    public bool ApplyOther { get; set; }

    public bool ApplyVat { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedMaterialProcessingPercent { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedAdminExpensesPercent { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedMinistryOfFinancePercent { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedWorkersFundPercent { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedOtherPercent { get; set; }

    [Range(0.0, 100.0)]
    public decimal? SelectedVatPercent { get; set; }

    public List<MaterialItemInputDto> MaterialItems { get; set; } = new();

    public List<MachineryItemInputDto> MachineryItems { get; set; } = new();
}
