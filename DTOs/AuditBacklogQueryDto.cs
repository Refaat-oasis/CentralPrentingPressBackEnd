namespace CentralPress.API.DTOs;

public class AuditBacklogQueryDto
{
    public string? Search { get; init; }
    public string? ModuleName { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
