using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class UpdateFormulaConfigDto
{
    public int FormulaConfigId { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "النسبة الافتراضية يجب أن تكون بين 0 و 100")]
    public decimal DefaultPercentage { get; set; }

    [MaxLength(100, ErrorMessage = "أسلوب حساب صندوق العاملين يجب ألا يتجاوز 100 حرف")]
    public string? WorkersFundBaseMode { get; set; }

    [MaxLength(200, ErrorMessage = "الخيارات المسموحة يجب ألا تتجاوز 200 حرف")]
    public string? AllowedOptionsCsv { get; set; }
}
