using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class HangfireOverviewViewModel
{
    public HangfireStatsViewModel Stats { get; init; } = new();
    public List<HangfireJobItemViewModel> Jobs { get; init; } = new();
}
