namespace CentralPress.API.ViewModels;

public class FormulaConfigViewModel
{
    public int FormulaConfigId { get; set; }
    public int StepOrder { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabelAr { get; set; } = string.Empty;
    public decimal DefaultPercentage { get; set; }
    public string? AllowedOptionsCsv { get; set; }
    public bool IsOptional { get; set; }
    public string? WorkersFundBaseMode { get; set; }
    public string? FormulaExpressionDescriptionAr { get; set; }
    public bool IsActive { get; set; }
}
