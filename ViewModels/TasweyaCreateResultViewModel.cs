using System.Collections.Generic;

namespace CentralPress.API.ViewModels;

public class TasweyaCreateResultViewModel
{
    public string Message { get; set; } = string.Empty;
    public int TasweyaId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal VatTotal { get; set; }
    public decimal SettlementGrandTotal { get; set; }
    public decimal RevenuesTotal { get; set; }
    public decimal TotalMaterialsSum { get; set; }
    public decimal TotalWorkerWagesSum { get; set; }
    public decimal TotalMachineCostsSum { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
}

public class BatchTasweyaCreateResultViewModel
{
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public List<TasweyaCreateResultViewModel> Items { get; set; } = new();
}
