using System;

namespace CentralPress.API.ViewModels;

public class CustomerMoqaysaSummaryViewModel
{
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public decimal FinalGrandTotal { get; set; }
    public bool IsIncludedInTasweya { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
