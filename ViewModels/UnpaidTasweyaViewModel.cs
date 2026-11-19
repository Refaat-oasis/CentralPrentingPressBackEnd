using System;

namespace CentralPress.API.ViewModels;

public class UnpaidTasweyaViewModel
{
    public int TasweyaId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateOnly SettlementDate { get; set; }
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerNameAr { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string JobOrderNumbersText { get; set; } = string.Empty;
    public decimal TotalMaterialsSum { get; set; }
    public decimal TotalWorkerWagesSum { get; set; }
    public decimal TotalMachineCostsSum { get; set; }
    public decimal AdminExpensesSum { get; set; }
    public decimal MarkupAmountSum { get; set; }
    public decimal MinistryOfFinanceSum { get; set; }
    public decimal WorkersFundSum { get; set; }
    public decimal OtherAmountSum { get; set; }
    public decimal RevenuesTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal SettlementGrandTotal { get; set; }
    public string TotalAmountInWordsAr { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
