namespace CentralPress.API.ViewModels;

public class MoqaysatDetailedTotalsViewModel
{
    public int Count { get; set; }
    public decimal TotalRawMaterials { get; set; }
    public decimal TotalMaterialProcessingFee { get; set; }
    public decimal TotalMaterials { get; set; }
    public decimal TotalWorkerWages { get; set; }
    public decimal TotalMachineCosts { get; set; }
    public decimal TotalAdminExpenses { get; set; }
    public decimal TotalMarkupAmount { get; set; }
    public decimal TotalMinistryOfFinanceFee { get; set; }
    public decimal TotalWorkersSyndicateFund { get; set; }
    public decimal TotalOtherAmount { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal TotalFinalGrandTotal { get; set; }
    public decimal FinalGrandTotal { get; set; }
}
