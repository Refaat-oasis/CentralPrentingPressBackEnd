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

        // 1. Place entered machinery items into their official fixed catalog DisplayOrder slots
        //    so partial selections (e.g. only طبع and قص) never scramble the template row order.
        var remainingItems = moqaysa.MachineryItems
            .OrderBy(mi => mi.Machinery != null ? mi.Machinery.DisplayOrder : int.MaxValue)
            .ThenBy(mi => mi.MachineryId)
            .ToList();

        static decimal ResolveMachineHours(MoqaysaMachineryItem item)
        {
            if (item.MachineHours > 0m) return item.MachineHours;
            if (item.TotalMachineOperatingCost > 0m)
            {
                return item.MachineHourlyRateSnapshot > 0m
                    ? Math.Round(item.TotalMachineOperatingCost / item.MachineHourlyRateSnapshot, 2, MidpointRounding.AwayFromZero)
                    : item.Hours;
            }
            return 0m;
        }

        foreach (var mac in activeCatalogMachineries)
        {
            var macName = mac.MachineryNameAr.Trim();
            var matchIdx = remainingItems.FindIndex(mi =>
                mi.MachineryId == mac.MachineryId ||
                string.Equals(
                    (!string.IsNullOrWhiteSpace(mi.MachineryNameSnapshot) ? mi.MachineryNameSnapshot : mi.Machinery?.MachineryNameAr)?.Trim(),
                    macName,
                    StringComparison.OrdinalIgnoreCase));

            if (matchIdx >= 0)
            {
                var item = remainingItems[matchIdx];
                remainingItems.RemoveAt(matchIdx);
                machineryLines.Add(new MachineryLine(
                    Name: !string.IsNullOrWhiteSpace(item.MachineryNameSnapshot) ? item.MachineryNameSnapshot : mac.MachineryNameAr,
                    Hours: item.Hours,
                    WorkerRate: item.WorkerHourlyRateSnapshot,
                    WorkerValue: item.TotalWorkerWages,
                    MachineHours: ResolveMachineHours(item),
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

        // 2. Append any historical/inactive/custom machinery items not in the active catalog
        foreach (var item in remainingItems)
        {
            machineryLines.Add(new MachineryLine(
                Name: !string.IsNullOrWhiteSpace(item.MachineryNameSnapshot) ? item.MachineryNameSnapshot : (item.Machinery?.MachineryNameAr ?? string.Empty),
                Hours: item.Hours,
                WorkerRate: item.WorkerHourlyRateSnapshot,
                WorkerValue: item.TotalWorkerWages,
                MachineHours: ResolveMachineHours(item),
                MachineRate: item.MachineHourlyRateSnapshot,
                MachineValue: item.TotalMachineOperatingCost));
        }

        // 3. Guarantee at least 11 rows if the catalog has fewer active entries
        while (machineryLines.Count < MoqaysaTokens.MachinerySlots)
        {
            machineryLines.Add(new MachineryLine(
                Name: string.Empty,
                Hours: 0m,
                WorkerRate: 0m,
                WorkerValue: 0m,
                MachineHours: 0m,
                MachineRate: 0m,
                MachineValue: 0m));
        }

        var materialLines = moqaysa.MaterialItems
            .OrderBy(m => m.ItemId)
            .Select(m => new MaterialLine(
                Name: m.MaterialNameSnapshot,
                Qty: m.Quantity,
                Price: m.UnitPrice,
                Total: m.TotalPrice))
            .ToList();

        var parsedQty = MoqaysaDraftPreviewRequest.ParseLeadingQuantity(moqaysa.RequiredQuantityText);
        var unitPriceStr = parsedQty > 0m
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
                    .ThenInclude(m => m!.Customer)
                        .ThenInclude(c => c!.Account)
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
                            .ThenInclude(m => m!.Customer)
                                .ThenInclude(c => c!.Account)
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

        var linkedMoqaysat = tasweya.MoqaysaLinks
            .Where(ml => ml.Moqaysa != null)
            .Select(ml => ml.Moqaysa!)
            .GroupBy(m => m.MoqaysaId)
            .Select(g => g.First())
            .ToList();

        var firstLinkedMoqaysa = linkedMoqaysat.FirstOrDefault();

        var jobOrdersDisplay = !string.IsNullOrWhiteSpace(tasweya.JobOrderNumbersText)
            ? tasweya.JobOrderNumbersText.Trim()
            : string.Join(" ، ", linkedMoqaysat
                .Select(m => m.JobOrderNumber)
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Distinct());

        var requesterName = !string.IsNullOrWhiteSpace(tasweya.Customer?.CustomerNameAr)
            ? tasweya.Customer.CustomerNameAr
            : (firstLinkedMoqaysa?.Customer?.CustomerNameAr ?? string.Empty);

        var finalRevenue = tasweya.RevenuesTotal;
        var finalVat = tasweya.VatTotal;
        var finalTotal = tasweya.SettlementGrandTotal;

        // Fallback only if a legacy/incomplete record stored 0 grand total
        if (finalTotal <= 0m && linkedMoqaysat.Count > 0)
        {
            finalRevenue = linkedMoqaysat.Sum(m => m.SubTotal3);
            finalVat = linkedMoqaysat.Sum(m => m.VatAmount);
            finalTotal = linkedMoqaysat.Sum(m => m.FinalGrandTotal);
        }

        var mergedMoqaysa = new MoqaysaPrintData(
            JobOrderNo: jobOrdersDisplay,
            Requester: requesterName,
            Quantity: "1",
            UnitPrice: finalTotal.ToString("0.00", CultureInfo.InvariantCulture),
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
            Subtotal: finalTotal,
            Ministry: tasweya.MinistryOfFinanceSum,
            WorkersFund: tasweya.WorkersFundSum,
            TotalA: finalTotal,
            Other: tasweya.OtherAmountSum,
            Revenue: finalRevenue,
            Vat: finalVat,
            Total: finalTotal);

        // Build authoritative debit account list across ALL linked moqaysat so
        // Single Account (Count == 1 -> Tasweya_Single.docx) vs Multi Account (Count > 1 -> Tasweya_Multi.docx)
        // is always accurate even when a Tasweya spans multiple moqaysat.
        var rawShareEntries = new List<(int AccountId, string AccountName, decimal Amount)>();
        foreach (var m in linkedMoqaysat)
        {
            var mShares = m.AccountShares?.Where(s => s.SharePercentage > 0m || s.AllocatedAmount > 0m).ToList();
            if (mShares != null && mShares.Count > 0)
            {
                foreach (var s in mShares)
                {
                    var name = s.Account?.AccountNameAr
                        ?? (s.AccountId == tasweya.AccountId ? tasweya.Account?.AccountNameAr : null)
                        ?? $"حساب #{s.AccountId}";
                    var amt = s.AllocatedAmount > 0m
                        ? s.AllocatedAmount
                        : Math.Round(m.FinalGrandTotal * (s.SharePercentage / 100m), 2, MidpointRounding.AwayFromZero);
                    rawShareEntries.Add((s.AccountId, name, amt));
                }
            }
            else
            {
                var fallbackAccId = m.Customer?.AccountId ?? tasweya.AccountId;
                var fallbackAccName = m.Customer?.Account?.AccountNameAr
                    ?? tasweya.Account?.AccountNameAr
                    ?? "العملاء";
                rawShareEntries.Add((fallbackAccId, fallbackAccName, m.FinalGrandTotal));
            }
        }

        var groupedAccounts = rawShareEntries
            .GroupBy(e => e.AccountId)
            .Select(g => (AccountId: g.Key, Name: g.First().AccountName, Amount: g.Sum(x => x.Amount)))
            .ToList();

        // Check if this Tasweya is a legacy single-account split where another active Tasweya covers the remaining share
        bool isSplitSingleAccountTasweya = false;
        if (groupedAccounts.Count > 1 && linkedMoqaysat.Count > 0)
        {
            var linkedIds = linkedMoqaysat.Select(m => m.MoqaysaId).ToList();
            bool hasOtherActiveTasweya = await context.TasweyaMoqaysaLinks
                .AsNoTracking()
                .AnyAsync(l => linkedIds.Contains(l.MoqaysaId)
                            && l.TasweyaId != tasweya.TasweyaId
                            && l.Tasweya != null
                            && l.Tasweya.IsActive, ct);
            decimal fullMoqaysatSum = linkedMoqaysat.Sum(m => m.FinalGrandTotal);
            if (hasOtherActiveTasweya && Math.Abs(finalTotal - fullMoqaysatSum) > 1m)
            {
                isSplitSingleAccountTasweya = true;
            }
        }

        var accounts = new List<AccountLine>();
        if (!isSplitSingleAccountTasweya && groupedAccounts.Count > 1)
        {
            decimal rawSum = groupedAccounts.Sum(a => a.Amount);
            decimal allocatedSum = 0m;
            for (int i = 0; i < groupedAccounts.Count; i++)
            {
                var acc = groupedAccounts[i];
                decimal amt;
                if (i == groupedAccounts.Count - 1)
                {
                    amt = Math.Round(finalTotal - allocatedSum, 2, MidpointRounding.AwayFromZero);
                }
                else if (rawSum > 0m)
                {
                    amt = Math.Round(finalTotal * (acc.Amount / rawSum), 2, MidpointRounding.AwayFromZero);
                    allocatedSum += amt;
                }
                else
                {
                    amt = Math.Round(finalTotal / groupedAccounts.Count, 2, MidpointRounding.AwayFromZero);
                    allocatedSum += amt;
                }
                accounts.Add(new AccountLine(acc.Name, amt));
            }
        }
        else if (!isSplitSingleAccountTasweya && groupedAccounts.Count == 1)
        {
            accounts.Add(new AccountLine(groupedAccounts[0].Name, finalTotal));
        }
        else
        {
            var singleName = tasweya.Account?.AccountNameAr
                ?? groupedAccounts.FirstOrDefault(a => a.AccountId == tasweya.AccountId).Name
                ?? groupedAccounts.FirstOrDefault().Name
                ?? "العملاء";
            accounts.Add(new AccountLine(singleName, finalTotal));
        }

        return new TasweyaPrintData(
            SettlementNo: tasweya.SettlementNumber,
            Date: tasweya.SettlementDate.ToDateTime(TimeOnly.MinValue),
            ClaimNo: tasweya.ClaimNumber,
            Moqaysa: mergedMoqaysa,
            Accounts: accounts);
    }

    public async Task<IReadOnlyList<int>> LoadLinkedMoqaysaIdsAsync(int tasweyaId, CancellationToken ct)
    {
        return await context.TasweyaMoqaysaLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TasweyaId == tasweyaId)
            .OrderBy(l => l.LinkId)
            .Select(l => l.MoqaysaId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> LoadActiveMachineryNamesAsync(CancellationToken ct)
    {
        return await context.Machineries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.MachineryId)
            .Select(m => m.MachineryNameAr)
            .ToListAsync(ct);
    }
}
