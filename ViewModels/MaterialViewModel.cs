namespace CentralPress.API.ViewModels;

public class MaterialViewModel
{
    public int MaterialId { get; set; }
    public string MaterialNameAr { get; set; } = string.Empty;
    public string? UnitNameAr { get; set; }
    public decimal StandardUnitPrice { get; set; }
    public string? CategoryAr { get; set; }
    public bool IsActive { get; set; }
}
