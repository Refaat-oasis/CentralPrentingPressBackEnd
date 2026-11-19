using System.Globalization;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Documents;

public sealed class EfPrintDataSource(ApplicationDbContext context) : IPrintDataSource
{
    public async Task<MoqaysaPrintData> LoadMoqaysaAsync(int moqaysaId, CancellationToken ct)
    {
        var moqaysa = await context.Moqaysat
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
                .ThenInclude(mi => mi.Machinery)
            .Include(m => m.AccountShares)
                .ThenInclude(s => s.Account)
            .FirstOrDefaultAsync(m => m.MoqaysaId == moqaysaId, ct);

        if (moqaysa == null)
            throw new KeyNotFoundException($"Moqaysa with ID {moqaysaId} was not found.");

        var activeCatalogMachineries = await context.Machineries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.MachineryId)
            .ToListAsync(ct);

        var machineryLines = new List<MachineryLine>();
        if (activeCatalogMachineries.Count > 0 && activeCatalogMachineries.Count <= MoqaysaTokens.MachinerySlots)
        {
            foreach (var mac in activeCatalogMachineries)
            {
                var item = moqaysa.MachineryItems.FirstOrDefault(i =>
                    i.MachineryId == mac.MachineryId ||
                    (!string.IsNullOrWhiteSpace(i.MachineryNameSnapshot) &&
                     string.Equals(i.MachineryNameSnapshot.Trim(), mac.MachineryNameAr.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (item != null)
                {
                    machineryLines.Add(new MachineryLine(
                        Name: !string.IsNullOrWhiteSpace(item.MachineryNameSnapshot) ? item.MachineryNameSnapshot : mac.MachineryNameAr,
                        Hours: item.Hours,
                        WorkerRate: item.WorkerHourlyRateSnapshot,
                        WorkerValue: item.TotalWorkerWages,
                        MachineHours: item.MachineHours,
                        MachineRate: item.MachineHourlyRateSnapshot,
                        MachineValue: item.TotalMachineOperatingCost));
                }
                else
                {
                    machineryLines.Add(new MachineryLine(
                        Name: mac.MachineryNameAr,
                        Hours: 0m,
                        WorkerRate: 0m,
                        WorkerValue: 0m,
                        MachineHours: 0m,
                        MachineRate: 0m,
                        MachineValue: 0m));
                }
            }
        }
        else
        {
            var orderedItems = moqaysa.MachineryItems
                .OrderBy(mi => mi.Machinery != null ? mi.Machinery.DisplayOrder : int.MaxValue)
                .ThenBy(mi => mi.MachineryId)
                .ToList();

            foreach (var item in orderedItems)
            {
                machineryLines.Add(new MachineryLine(
                    Name: item.MachineryNameSnapshot,
                    Hours: item.Hours,
                    WorkerRate: item.WorkerHourlyRateSnapshot,
                    WorkerValue: item.TotalWorkerWages,
                    MachineHours: item.MachineHours,
                    MachineRate: item.MachineHourlyRateSnapshot,
                    MachineValue: item.TotalMachineOperatingCost));
            }
        }

        var materialLines = moqaysa.MaterialItems
            .Select(m => new MaterialLine(
                Name: m.MaterialNameSnapshot,
                Qty: m.Quantity,
                Price: m.UnitPrice,
                Total: m.TotalPrice))
            .ToList();

        var unitPriceStr = decimal.TryParse(moqaysa.RequiredQuantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedQty) && parsedQty > 0
            ? (moqaysa.FinalGrandTotal / parsedQty).ToString("0.00", CultureInfo.InvariantCulture)
            : moqaysa.FinalGrandTotal.ToString("0.00", CultureInfo.InvariantCulture);

        var requester = moqaysa.Customer?.CustomerNameAr ?? string.Empty;

        return new MoqaysaPrintData(
            JobOrderNo: moqaysa.JobOrderNumber,
            Requester: requester,
            Quantity: moqaysa.RequiredQuantityText,
            UnitPrice: unitPriceStr,
            Specs: moqaysa.SpecificationsText,
            Machinery: machineryLines,
            Materials: materialLines,
            LabourTotal: moqaysa.TotalWorkerWages,
            MachineryTotal: moqaysa.TotalMachineCosts,
            MatGross: moqaysa.RawMaterials,
            MatReturns: 0m,
            MatNet: moqaysa.RawMaterials,
            OpsMaterials: moqaysa.MaterialProcessingFee,
            OpsFilms: moqaysa.FilmsProcessingFee,
            MaterialsTotal: moqaysa.TotalMaterials,
            DirectCost: moqaysa.SubTotal1,
            Admin: moqaysa.AdminExpenses,
            ExpensesTotal: moqaysa.TotalExpenses,
            Percentage: moqaysa.MarkupAmount,
            Subtotal: moqaysa.TotalAfterMarkup,
            Ministry: moqaysa.MinistryOfFinanceFee,
            WorkersFund: moqaysa.WorkersSyndicateFund,
            TotalA: moqaysa.SubTotal2,
            Other: moqaysa.OtherAmount,
            Revenue: moqaysa.SubTotal3,
            Vat: moqaysa.VatAmount,
            Total: moqaysa.FinalGrandTotal);
    }

    public async Task<TasweyaPrintData> LoadTasweyaAsync(int tasweyaOrMoqaysaId, CancellationToken ct)
    {
        var tasweya = await context.Tasweyat
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .Include(t => t.MoqaysaLinks)
                .ThenInclude(ml => ml.Moqaysa)
                    .ThenInclude(m => m!.AccountShares)
                        .ThenInclude(s => s.Account)
            .FirstOrDefaultAsync(t => t.TasweyaId == tasweyaOrMoqaysaId, ct);

        if (tasweya == null)
        {
            // Try resolving by MoqaysaId
            var link = await context.TasweyaMoqaysaLinks
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.MoqaysaId == tasweyaOrMoqaysaId, ct);

            if (link != null)
            {
                tasweya = await context.Tasweyat
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(t => t.Account)
                    .Include(t => t.Customer)
                    .Include(t => t.MoqaysaLinks)
                        .ThenInclude(ml => ml.Moqaysa)
                            .ThenInclude(m => m!.AccountShares)
                                .ThenInclude(s => s.Account)
                    .FirstOrDefaultAsync(t => t.TasweyaId == link.TasweyaId, ct);
            }
        }

        if (tasweya == null)
        {
            // Check if Moqaysa exists before attempting synthesis
            var moqaysaExists = await context.Moqaysat
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(m => m.MoqaysaId == tasweyaOrMoqaysaId, ct);

            if (!moqaysaExists)
            {
                throw new KeyNotFoundException($"Neither Tasweya nor Moqaysa with ID {tasweyaOrMoqaysaId} was not found.");
            }

            // Synthesize direct preview from Moqaysa if no Tasweya is saved yet
            var directMoqaysa = await LoadMoqaysaAsync(tasweyaOrMoqaysaId, ct);
            return new TasweyaPrintData(
                SettlementNo: directMoqaysa.JobOrderNo,
                Date: DateTime.Today,
                ClaimNo: directMoqaysa.JobOrderNo,
                Moqaysa: directMoqaysa,
                Accounts: new List<AccountLine>
                {
                    new("العملاء", directMoqaysa.Total)
                });
        }

        var linkedMoqaysa = tasweya.MoqaysaLinks
            .Select(ml => ml.Moqaysa)
            .FirstOrDefault(m => m != null);

        var baseMoqaysa = linkedMoqaysa != null
            ? await LoadMoqaysaAsync(linkedMoqaysa.MoqaysaId, ct)
            : new MoqaysaPrintData(
                JobOrderNo: tasweya.JobOrderNumbersText,
                Requester: tasweya.Customer?.CustomerNameAr ?? string.Empty,
                Quantity: "1",
                UnitPrice: tasweya.SettlementGrandTotal.ToString("0.00", CultureInfo.InvariantCulture),
                Specs: string.Empty,
                Machinery: Array.Empty<MachineryLine>(),
                Materials: Array.Empty<MaterialLine>(),
                LabourTotal: tasweya.TotalWorkerWagesSum,
                MachineryTotal: tasweya.TotalMachineCostsSum,
                MatGross: tasweya.TotalMaterialsSum,
                MatReturns: 0m,
                MatNet: tasweya.TotalMaterialsSum,
                OpsMaterials: 0m,
                OpsFilms: 0m,
                MaterialsTotal: tasweya.TotalMaterialsSum,
                DirectCost: tasweya.TotalMaterialsSum + tasweya.TotalWorkerWagesSum + tasweya.TotalMachineCostsSum,
                Admin: tasweya.AdminExpensesSum,
                ExpensesTotal: tasweya.TotalMaterialsSum + tasweya.TotalWorkerWagesSum + tasweya.TotalMachineCostsSum + tasweya.AdminExpensesSum,
                Percentage: tasweya.MarkupAmountSum,
                Subtotal: tasweya.SettlementGrandTotal,
                Ministry: tasweya.MinistryOfFinanceSum,
                WorkersFund: tasweya.WorkersFundSum,
                TotalA: tasweya.SettlementGrandTotal,
                Other: tasweya.OtherAmountSum,
                Revenue: tasweya.RevenuesTotal,
                Vat: tasweya.VatTotal,
                Total: tasweya.SettlementGrandTotal);

        // Harmonize moqaysa totals with tasweya's stored figures if non-zero
        var finalRevenue = tasweya.RevenuesTotal > 0 ? tasweya.RevenuesTotal : baseMoqaysa.Revenue;
        var finalVat = tasweya.VatTotal > 0 ? tasweya.VatTotal : baseMoqaysa.Vat;
        var finalTotal = tasweya.SettlementGrandTotal > 0 ? tasweya.SettlementGrandTotal : baseMoqaysa.Total;

        var mergedMoqaysa = baseMoqaysa with
        {
            MaterialsTotal = tasweya.TotalMaterialsSum > 0 ? tasweya.TotalMaterialsSum : baseMoqaysa.MaterialsTotal,
            LabourTotal = tasweya.TotalWorkerWagesSum > 0 ? tasweya.TotalWorkerWagesSum : baseMoqaysa.LabourTotal,
            MachineryTotal = tasweya.TotalMachineCostsSum > 0 ? tasweya.TotalMachineCostsSum : baseMoqaysa.MachineryTotal,
            Admin = tasweya.AdminExpensesSum > 0 ? tasweya.AdminExpensesSum : baseMoqaysa.Admin,
            Percentage = tasweya.MarkupAmountSum > 0 ? tasweya.MarkupAmountSum : baseMoqaysa.Percentage,
            Ministry = tasweya.MinistryOfFinanceSum > 0 ? tasweya.MinistryOfFinanceSum : baseMoqaysa.Ministry,
            WorkersFund = tasweya.WorkersFundSum > 0 ? tasweya.WorkersFundSum : baseMoqaysa.WorkersFund,
            Other = tasweya.OtherAmountSum,
            Revenue = finalRevenue,
            Vat = finalVat,
            Total = finalTotal
        };

        // Build account shares
        var accounts = new List<AccountLine>();
        var shares = linkedMoqaysa?.AccountShares?.Where(s => s.SharePercentage > 0).ToList();
        if (shares != null && shares.Count > 1)
        {
            decimal allocatedSum = 0m;
            for (int i = 0; i < shares.Count; i++)
            {
                var s = shares[i];
                var name = s.Account?.AccountNameAr ?? $"حساب #{s.AccountId}";
                decimal amt;
                if (i == shares.Count - 1)
                {
                    amt = finalTotal - allocatedSum;
                }
                else
                {
                    amt = Math.Round(finalTotal * (s.SharePercentage / 100m), 2, MidpointRounding.AwayFromZero);
                    allocatedSum += amt;
                }
                accounts.Add(new AccountLine(name, amt));
            }
        }
        else
        {
            var singleName = tasweya.Account?.AccountNameAr ?? linkedMoqaysa?.AccountShares?.FirstOrDefault()?.Account?.AccountNameAr ?? "العملاء";
            accounts.Add(new AccountLine(singleName, finalTotal));
        }

        return new TasweyaPrintData(
            SettlementNo: tasweya.SettlementNumber,
            Date: tasweya.SettlementDate.ToDateTime(TimeOnly.MinValue),
            ClaimNo: tasweya.ClaimNumber,
            Moqaysa: mergedMoqaysa,
            Accounts: accounts);
    }
}
