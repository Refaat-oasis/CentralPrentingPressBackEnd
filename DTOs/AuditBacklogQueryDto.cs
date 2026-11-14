using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class AuditBacklogQueryDto
{
    [MaxLength(200)]
    public string? Search { get; init; }

    [MaxLength(100)]
    public string? ModuleName { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 50;
}
