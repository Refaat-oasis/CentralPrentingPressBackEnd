namespace CentralPress.API.ViewModels;

public class HangfireStatsViewModel
{
    public long Enqueued { get; init; }
    public long Scheduled { get; init; }
    public long Processing { get; init; }
    public long Succeeded { get; init; }
    public long Failed { get; init; }
    public long Deleted { get; init; }
    public long ServersCount { get; init; }
}
