namespace CentralPress.API.ViewModels;

public class TasweyaCreateResultViewModel
{
    public string Message { get; set; } = string.Empty;
    public int TasweyaId { get; set; }
    public decimal VatTotal { get; set; }
    public decimal SettlementGrandTotal { get; set; }
    public decimal RevenuesTotal { get; set; }
    public decimal TotalMaterialsSum { get; set; }
    public decimal TotalWorkerWagesSum { get; set; }
    public decimal TotalMachineCostsSum { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
}
