namespace CentralPress.API.ViewModels;

public class OperationStatusViewModel
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}
