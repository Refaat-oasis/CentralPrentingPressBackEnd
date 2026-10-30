using System;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.Entities;

public class AuditBacklog
{
    [Key]
    public long AuditId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(250)]
    public string? EmployeeName { get; set; }

    [MaxLength(150)]
    public string? JobTitleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty; // Create, Update, ToggleActive, ConfirmPaymentOrder, Login, ExportWord, ExportPdf

    [MaxLength(100)]
    public string? ModuleName { get; set; }

    [MaxLength(150)]
    public string? EntityName { get; set; }

    [MaxLength(100)]
    public string? RecordId { get; set; }

    [MaxLength(500)]
    public string? RequestPath { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
