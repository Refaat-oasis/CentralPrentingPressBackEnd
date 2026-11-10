namespace CentralPress.API.ViewModels;

public class MoqaysaMaterialItemViewModel
{
    public int ItemId { get; set; }
    public int MoqaysaId { get; set; }
    public int? MaterialId { get; set; }
    public string MaterialNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
