namespace CentralPress.API.ViewModels;

public class JobTitleSummaryViewModel
{
    public int JobTitleId { get; init; }
    public string TitleNameAr { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int EmployeesCount { get; init; }
}
