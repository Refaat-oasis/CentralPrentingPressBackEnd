using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class CostingFormulaConfig
{
    [Key]
    public int FormulaConfigId { get; set; }

    public int StepOrder { get; set; }

    [Required]
    [MaxLength(50)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FieldLabelAr { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,4)")]
    public decimal DefaultPercentage { get; set; }

    [MaxLength(100)]
    public string? AllowedOptionsCsv { get; set; }

    public bool IsOptional { get; set; }

    [MaxLength(50)]
    public string WorkersFundBaseMode { get; set; } = "DiffTotalMinusMaterials"; // DiffTotalMinusMaterials or MinistryOfFinanceValue

    [MaxLength(300)]
    public string? FormulaExpressionDescriptionAr { get; set; }

    public bool IsActive { get; set; } = true;
}
