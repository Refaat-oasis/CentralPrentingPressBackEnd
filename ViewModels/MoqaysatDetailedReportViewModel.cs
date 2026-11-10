using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class MoqaysatDetailedReportViewModel
{
    public MoqaysatDetailedTotalsViewModel Totals { get; set; } = new();
    public List<MoqaysaDetailedItemViewModel> Items { get; set; } = new();
}
