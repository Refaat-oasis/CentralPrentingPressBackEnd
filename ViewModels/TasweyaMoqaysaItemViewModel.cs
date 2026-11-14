namespace CentralPress.API.ViewModels;

public class TasweyaMoqaysaItemViewModel
{
    public int LinkId { get; set; }
    public int MoqaysaId { get; set; }
    public string JobOrderNumber { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; } = 100m;
    public decimal AllocatedAmount { get; set; }
    public decimal FinalGrandTotal { get; set; }
    public bool ApplyVat { get; set; }
    public decimal VatAmount { get; set; }
}
