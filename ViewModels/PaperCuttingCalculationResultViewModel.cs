namespace CentralPress.API.ViewModels;

public class PaperCuttingCalculationResultViewModel
{
    public decimal ParentSheetWidthMm { get; set; }
    public decimal ParentSheetHeightMm { get; set; }
    public decimal CutItemWidthMm { get; set; }
    public decimal CutItemHeightMm { get; set; }

    public int DirectCutsPerSheet { get; set; }
    public int RotatedCutsPerSheet { get; set; }
    public int BestCutsPerSheet { get; set; }
    public string BestOrientation { get; set; } = "Direct";

    public decimal SheetEfficiencyPercent { get; set; }
    public decimal WastePercent { get; set; }

    public int RequiredItemCopies { get; set; }
    public int NetSheetsNeeded { get; set; }
    public int SetupSpoilageSheets { get; set; }
    public int RunSpoilageSheets { get; set; }
    public int TotalSpoilageSheets { get; set; }
    public int GrossSheetsNeeded { get; set; }

    public decimal ReamsNeeded { get; set; }
    public decimal Packs1000Needed { get; set; }
}
