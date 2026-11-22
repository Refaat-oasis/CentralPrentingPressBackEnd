using System.Globalization;
using System.Text.RegularExpressions;

namespace CentralPress.API.Documents;

public sealed record DraftMaterialItemDto(
    string? MaterialNameSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record DraftMachineryItemDto(
    string? MachineryNameSnapshot,
    decimal Hours,
    decimal? MachineHours,
    decimal WorkerHourlyRateSnapshot,
    decimal TotalWorkerWages,
    decimal MachineHourlyRateSnapshot,
    decimal TotalMachineOperatingCost);

public sealed record MoqaysaDraftPreviewRequest(
    string? JobOrderNumber,
    string? CustomerName,
    string? RequiredQuantityText,
    string? SpecificationsText,
    List<DraftMaterialItemDto>? MaterialItems,
    List<DraftMachineryItemDto>? MachineryItems,
    decimal RawMaterials,
    decimal MaterialProcessingFee,
    decimal FilmsProcessingFee,
    decimal TotalMaterials,
    decimal TotalWorkerWages,
    decimal TotalMachineCosts,
    decimal SubTotal1,
    decimal AdminExpenses,
    decimal TotalExpenses,
    decimal MarkupAmount,
    decimal TotalAfterMarkup,
    decimal MinistryOfFinanceFee,
    decimal WorkersSyndicateFund,
    decimal SubTotal2,
    decimal OtherAmount,
    decimal SubTotal3,
    decimal VatAmount,
    decimal FinalGrandTotal)
{
    public MoqaysaPrintData ToPrintData(IReadOnlyList<string>? activeCatalogMachineryNames = null)
    {
        var remainingDraftItems = (MachineryItems ?? []).ToList();
        var machineryLines = new List<MachineryLine>();

        if (activeCatalogMachineryNames != null && activeCatalogMachineryNames.Count > 0)
        {
            foreach (var catalogName in activeCatalogMachineryNames)
            {
                var trimmedCatalog = catalogName?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedCatalog)) continue;

                var matchIdx = remainingDraftItems.FindIndex(m =>
                    string.Equals(m.MachineryNameSnapshot?.Trim(), trimmedCatalog, StringComparison.OrdinalIgnoreCase));

                if (matchIdx >= 0)
                {
                    var m = remainingDraftItems[matchIdx];
                    remainingDraftItems.RemoveAt(matchIdx);
                    machineryLines.Add(new MachineryLine(
                        Name: !string.IsNullOrWhiteSpace(m.MachineryNameSnapshot) ? m.MachineryNameSnapshot.Trim() : trimmedCatalog,
                        Hours: m.Hours,
                        WorkerRate: m.WorkerHourlyRateSnapshot,
                        WorkerValue: m.TotalWorkerWages,
                        MachineHours: m.MachineHours ?? m.Hours,
                        MachineRate: m.MachineHourlyRateSnapshot,
                        MachineValue: m.TotalMachineOperatingCost));
                }
                else
                {
                    machineryLines.Add(new MachineryLine(trimmedCatalog, 0m, 0m, 0m, 0m, 0m, 0m));
                }
            }
        }

        foreach (var m in remainingDraftItems)
        {
            machineryLines.Add(new MachineryLine(
                Name: m.MachineryNameSnapshot ?? string.Empty,
                Hours: m.Hours,
                WorkerRate: m.WorkerHourlyRateSnapshot,
                WorkerValue: m.TotalWorkerWages,
                MachineHours: m.MachineHours ?? m.Hours,
                MachineRate: m.MachineHourlyRateSnapshot,
                MachineValue: m.TotalMachineOperatingCost));
        }

        while (machineryLines.Count < MoqaysaTokens.MachinerySlots)
        {
            machineryLines.Add(new MachineryLine(string.Empty, 0m, 0m, 0m, 0m, 0m, 0m));
        }

        var materialLines = (MaterialItems ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.MaterialNameSnapshot) || m.Quantity > 0m || m.TotalPrice > 0m)
            .Select(m => new MaterialLine(
                Name: m.MaterialNameSnapshot ?? string.Empty,
                Qty: m.Quantity,
                Price: m.UnitPrice,
                Total: m.TotalPrice))
            .ToList();

        decimal parsedQty = ParseLeadingQuantity(RequiredQuantityText);
        var unitPriceStr = parsedQty > 0m
            ? (FinalGrandTotal / parsedQty).ToString("0.00", CultureInfo.InvariantCulture)
            : FinalGrandTotal.ToString("0.00", CultureInfo.InvariantCulture);

        return new MoqaysaPrintData(
            JobOrderNo: string.IsNullOrWhiteSpace(JobOrderNumber) ? "مسودة" : JobOrderNumber.Trim(),
            Requester: CustomerName?.Trim() ?? string.Empty,
            Quantity: RequiredQuantityText?.Trim() ?? string.Empty,
            UnitPrice: unitPriceStr,
            Specs: SpecificationsText?.Trim() ?? string.Empty,
            Machinery: machineryLines,
            Materials: materialLines,
            LabourTotal: TotalWorkerWages,
            MachineryTotal: TotalMachineCosts,
            MatGross: RawMaterials,
            MatReturns: 0m,
            MatNet: RawMaterials,
            OpsMaterials: MaterialProcessingFee,
            OpsFilms: FilmsProcessingFee,
            MaterialsTotal: TotalMaterials,
            DirectCost: SubTotal1,
            Admin: AdminExpenses,
            ExpensesTotal: TotalExpenses,
            Percentage: MarkupAmount,
            Subtotal: TotalAfterMarkup,
            Ministry: MinistryOfFinanceFee,
            WorkersFund: WorkersSyndicateFund,
            TotalA: SubTotal2,
            Other: OtherAmount,
            Revenue: SubTotal3,
            Vat: VatAmount,
            Total: FinalGrandTotal);
    }

    internal static decimal ParseLeadingQuantity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        var clean = text.Replace(",", "").Replace("،", "").Trim();
        var match = Regex.Match(clean, @"\d+(\.\d+)?");
        return match.Success && decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var q)
            ? q
            : 0m;
    }
}

public sealed record DraftAccountShareDto(
    string? AccountName,
    decimal AllocatedAmount);

public sealed record TasweyaDraftPreviewRequest(
    string? SettlementNumber,
    string? SettlementDate,
    string? ClaimNumber,
    string? AccountDebitHeaderText,
    string? CustomerName,
    string? JobOrderNumbersText,
    decimal TotalMaterialsSum,
    decimal TotalWorkerWagesSum,
    decimal TotalMachineCostsSum,
    decimal AdminExpensesSum,
    decimal MarkupAmountSum,
    decimal MinistryOfFinanceSum,
    decimal WorkersFundSum,
    decimal OtherAmountSum,
    decimal RevenuesTotal,
    decimal VatTotal,
    decimal SettlementGrandTotal,
    List<DraftAccountShareDto>? AccountShares)
{
    public TasweyaPrintData ToPrintData()
    {
        var parsedDate = DateTime.TryParse(SettlementDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.Today;

        var total = Math.Round(SettlementGrandTotal, 2, MidpointRounding.AwayFromZero);
        var vat = Math.Round(VatTotal, 2, MidpointRounding.AwayFromZero);
        var rev = Math.Round(RevenuesTotal, 2, MidpointRounding.AwayFromZero);
        if (Math.Abs((rev + vat) - total) <= 0.05m && rev + vat != total)
        {
            rev = total - vat;
        }

        var accounts = new List<AccountLine>();
        var validShares = (AccountShares ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.AccountName))
            .ToList();

        if (validShares.Count > 1)
        {
            decimal rawSum = validShares.Sum(s => s.AllocatedAmount);
            decimal running = 0m;
            for (int i = 0; i < validShares.Count; i++)
            {
                decimal amt;
                if (i == validShares.Count - 1)
                {
                    amt = Math.Round(total - running, 2, MidpointRounding.AwayFromZero);
                }
                else if (rawSum > 0m)
                {
                    amt = Math.Round(total * (validShares[i].AllocatedAmount / rawSum), 2, MidpointRounding.AwayFromZero);
                    running += amt;
                }
                else
                {
                    amt = Math.Round(total / validShares.Count, 2, MidpointRounding.AwayFromZero);
                    running += amt;
                }
                accounts.Add(new AccountLine(validShares[i].AccountName!.Trim(), amt));
            }
        }
        else
        {
            var rawHeader = validShares.FirstOrDefault()?.AccountName
                ?? AccountDebitHeaderText
                ?? "العملاء";
            var cleanName = Regex.Replace(rawHeader.Trim(), @"^من\s+حـ\s*/\s*", "").Trim();
            if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "العملاء";
            accounts.Add(new AccountLine(cleanName, total));
        }

        var moqaysaSummary = new MoqaysaPrintData(
            JobOrderNo: JobOrderNumbersText?.Trim() ?? string.Empty,
            Requester: CustomerName?.Trim() ?? string.Empty,
            Quantity: "1",
            UnitPrice: total.ToString("0.00", CultureInfo.InvariantCulture),
            Specs: string.Empty,
            Machinery: Array.Empty<MachineryLine>(),
            Materials: Array.Empty<MaterialLine>(),
            LabourTotal: TotalWorkerWagesSum,
            MachineryTotal: TotalMachineCostsSum,
            MatGross: TotalMaterialsSum,
            MatReturns: 0m,
            MatNet: TotalMaterialsSum,
            OpsMaterials: 0m,
            OpsFilms: 0m,
            MaterialsTotal: TotalMaterialsSum,
            DirectCost: TotalMaterialsSum + TotalWorkerWagesSum + TotalMachineCostsSum,
            Admin: AdminExpensesSum,
            ExpensesTotal: TotalMaterialsSum + TotalWorkerWagesSum + TotalMachineCostsSum + AdminExpensesSum,
            Percentage: MarkupAmountSum,
            Subtotal: total,
            Ministry: MinistryOfFinanceSum,
            WorkersFund: WorkersFundSum,
            TotalA: total,
            Other: OtherAmountSum,
            Revenue: rev,
            Vat: vat,
            Total: total);

        return new TasweyaPrintData(
            SettlementNo: string.IsNullOrWhiteSpace(SettlementNumber) ? "مسودة" : SettlementNumber.Trim(),
            Date: parsedDate,
            ClaimNo: ClaimNumber?.Trim() ?? string.Empty,
            Moqaysa: moqaysaSummary,
            Accounts: accounts);
    }
}
