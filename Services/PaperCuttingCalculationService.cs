using System;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public class PaperCuttingCalculationService : IPaperCuttingCalculationService
{
    public PaperCuttingCalculationResultViewModel CalculateCuttingYield(PaperCuttingCalculationRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ParentSheetWidthMm <= 0 || request.ParentSheetHeightMm <= 0)
            throw new ArgumentException("أبعاد فرخ الورق يجب أن تكون أكبر من الصفر");
        if (request.CutItemWidthMm <= 0 || request.CutItemHeightMm <= 0)
            throw new ArgumentException("أبعاد المنتج المطلوب قصه يجب أن تكون أكبر من الصفر");
        if (request.RequiredItemCopies <= 0)
            throw new ArgumentException("عدد النسخ المطلوبة يجب أن يكون أكبر من الصفر");

        decimal trimming = Math.Max(0m, request.TrimmingMarginMm);
        decimal effectiveWidth = request.CutItemWidthMm + (2 * trimming);
        decimal effectiveHeight = request.CutItemHeightMm + (2 * trimming);

        // Direct orientation
        int directAcross = (int)Math.Floor(request.ParentSheetWidthMm / effectiveWidth);
        int directDown = (int)Math.Floor(request.ParentSheetHeightMm / effectiveHeight);
        int directYield = Math.Max(0, directAcross * directDown);

        // Rotated orientation (90 degrees)
        int rotatedAcross = (int)Math.Floor(request.ParentSheetWidthMm / effectiveHeight);
        int rotatedDown = (int)Math.Floor(request.ParentSheetHeightMm / effectiveWidth);
        int rotatedYield = Math.Max(0, rotatedAcross * rotatedDown);

        int bestYield = Math.Max(directYield, rotatedYield);
        string bestOrientation = (directYield >= rotatedYield) ? "Direct" : "Rotated";

        decimal parentArea = request.ParentSheetWidthMm * request.ParentSheetHeightMm;
        decimal cutItemNetArea = request.CutItemWidthMm * request.CutItemHeightMm;
        decimal usedArea = bestYield * cutItemNetArea;
        decimal efficiencyPercent = parentArea > 0
            ? Math.Round((usedArea / parentArea) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        decimal wastePercent = Math.Max(0m, 100m - efficiencyPercent);

        // BUG-M2-03 Fix: Use safe 64-bit (long) arithmetic to prevent 32-bit integer overflow
        long netSheetsLong = bestYield > 0
            ? (long)Math.Ceiling((decimal)request.RequiredItemCopies / bestYield)
            : 0L;

        long setupSpoilageLong = Math.Max(0L, (long)request.SetupSpoilageSheets);
        decimal runSpoilageRate = Math.Max(0m, request.RunSpoilagePercent) / 100m;
        long runSpoilageLong = (long)Math.Ceiling((decimal)netSheetsLong * runSpoilageRate);
        long totalSpoilageLong = setupSpoilageLong + runSpoilageLong;
        long grossSheetsLong = netSheetsLong + totalSpoilageLong;

        // Guard against 32-bit integer overflow by clamping to int.MaxValue
        int netSheets = (int)Math.Min((long)int.MaxValue, netSheetsLong);
        int setupSpoilage = (int)Math.Min((long)int.MaxValue, setupSpoilageLong);
        int runSpoilage = (int)Math.Min((long)int.MaxValue, runSpoilageLong);
        int totalSpoilage = (int)Math.Min((long)int.MaxValue, totalSpoilageLong);
        int grossSheets = (int)Math.Min((long)int.MaxValue, grossSheetsLong);

        // BUG-M2-04 Fix: Enforce Egyptian commercial accounting standard MidpointRounding.AwayFromZero
        decimal reams = Math.Round((decimal)grossSheets / 500m, 2, MidpointRounding.AwayFromZero);
        decimal packs1000 = Math.Round((decimal)grossSheets / 1000m, 2, MidpointRounding.AwayFromZero);

        return new PaperCuttingCalculationResultViewModel
        {
            ParentSheetWidthMm = request.ParentSheetWidthMm,
            ParentSheetHeightMm = request.ParentSheetHeightMm,
            CutItemWidthMm = request.CutItemWidthMm,
            CutItemHeightMm = request.CutItemHeightMm,
            DirectCutsPerSheet = directYield,
            RotatedCutsPerSheet = rotatedYield,
            BestCutsPerSheet = bestYield,
            BestOrientation = bestOrientation,
            SheetEfficiencyPercent = efficiencyPercent,
            WastePercent = wastePercent,
            RequiredItemCopies = request.RequiredItemCopies,
            NetSheetsNeeded = netSheets,
            SetupSpoilageSheets = setupSpoilage,
            RunSpoilageSheets = runSpoilage,
            TotalSpoilageSheets = totalSpoilage,
            GrossSheetsNeeded = grossSheets,
            ReamsNeeded = reams,
            Packs1000Needed = packs1000
        };
    }
}
