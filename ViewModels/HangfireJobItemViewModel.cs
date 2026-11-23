using System;

namespace CentralPress.API.ViewModels;

public class HangfireJobItemViewModel
{
    public string JobId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public string? RecipientEmail { get; init; }
    public string? Subject { get; init; }
    public int RetryCount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? NextExecutionUtc { get; init; }
    public DateTime? LastStateChangedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
}
