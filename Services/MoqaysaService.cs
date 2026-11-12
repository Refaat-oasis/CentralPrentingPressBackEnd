using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class MoqaysaService : IMoqaysaService
{
    private readonly ApplicationDbContext _context;
    private readonly MoqaysaCalculationService _calculationService;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;

    public MoqaysaService(
        ApplicationDbContext context,
        MoqaysaCalculationService calculationService,
        WordExportService wordExportService,
        PdfExportService pdfExportService)
    {
        _context = context;
        _calculationService = calculationService;
        _wordExportService = wordExportService;
        _pdfExportService = pdfExportService;
    }

    public static decimal ParseQuantity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1.0m;
        var cleanText = text.Replace(",", "").Replace("،", "").Replace("٬", "");
        var normalized = new string(cleanText.Select(c => c >= '٠' && c <= '٩' ? (char)('0' + (c - '٠')) : c).ToArray());
        var match = Regex.Match(normalized, @"\d+(\.\d+)?");
        if (match.Success && decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) && qty > 0)
        {
            return qty;
        }
        return 1.0m;
    }

    public async Task<MoqaysaCalculationResultViewModel> CalculatePreviewAsync(MoqaysaSaveDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var calc = await PerformCalculationInternalAsync(dto, ct);

        return new MoqaysaCalculationResultViewModel
        {
            RawMaterials = calc.RawMaterials,
            MaterialProcessingPercent = calc.MaterialProcessingPercent,
            MaterialProcessingFee = calc.MaterialProcessingFee,
            FilmsProcessingFee = calc.FilmsProcessingFee,
            TotalMaterials = calc.TotalMaterials,
            TotalWorkerWages = calc.TotalWorkerWages,
            TotalMachineCosts = calc.TotalMachineCosts,
            SubTotal1 = calc.SubTotal1,
            ApplyAdminExpenses = calc.ApplyAdminExpenses,
            AdminExpensesPercent = calc.AdminExpensesPercent,
            AdminExpenses = calc.AdminExpenses,
            TotalExpenses = calc.TotalExpenses,
            ApplyMarkup = calc.ApplyMarkup,
            SelectedMarkupPercent = calc.SelectedMarkupPercent,
            MarkupAmount = calc.MarkupAmount,
            TotalAfterMarkup = calc.TotalAfterMarkup,
            MinistryOfFinancePercent = calc.MinistryOfFinancePercent,
            MinistryOfFinanceFee = calc.MinistryOfFinanceFee,
            WorkersFundPercent = calc.WorkersFundPercent,
            WorkersFundBaseMode = calc.WorkersFundBaseMode,
            WorkersSyndicateFund = calc.WorkersSyndicateFund,
            SubTotal2 = calc.SubTotal2,
            ApplyOther = calc.ApplyOther,
            OtherPercent = calc.OtherPercent,
            OtherAmount = calc.OtherAmount,
            SubTotal3 = calc.SubTotal3,
            ApplyVat = calc.ApplyVat,
            VatPercent = calc.VatPercent,
            VatAmount = calc.VatAmount,
            FinalGrandTotal = calc.FinalGrandTotal,
            ParsedQuantity = calc.ParsedQuantity,
            UnitPriceBeforeVat = calc.UnitPriceBeforeVat,
            FinalUnitPrice = calc.FinalUnitPrice,
            TafqeetArabicText = calc.TafqeetArabicText
        };
    }

    public async Task<List<MoqaysaSummaryViewModel>> GetAllAsync(
        int? customerId = null,
        string? jobOrderNumber = null,
        bool? isIncludedInTasweya = null,
        bool? isPaid = null,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.Moqaysat
            .IgnoreQueryFilters()
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Include(m => m.AccountShares)
                .ThenInclude(s => s.Account)
            .AsQueryable();

        if (customerId.HasValue) query = query.Where(m => m.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(jobOrderNumber)) query = query.Where(m => m.JobOrderNumber.Contains(jobOrderNumber));
        if (isIncludedInTasweya.HasValue) query = query.Where(m => m.IsIncludedInTasweya == isIncludedInTasweya.Value);
        if (isPaid.HasValue) query = query.Where(m => m.IsPaid == isPaid.Value);
        if (activeOnly) query = query.Where(m => m.IsActive);

        var list = await query
            .OrderByDescending(m => m.MoqaysaId)
            .ToListAsync(ct);

        return list.Select(m =>
        {
            var shareVms = m.AccountShares.Select(s => new MoqaysaAccountShareViewModel
            {
                AccountId = s.AccountId,
                AccountNameAr = s.Account?.AccountNameAr ?? string.Empty,
                DebitHeaderTextAr = s.Account?.DebitHeaderTextAr ?? string.Empty,
                SharePercentage = s.SharePercentage,
                AllocatedAmount = s.AllocatedAmount
            }).ToList();

            string summaryText = shareVms.Count > 0
                ? string.Join(" ، ", shareVms.Select(s => $"{s.AccountNameAr} ({s.SharePercentage}%)"))
                : string.Empty;

            return new MoqaysaSummaryViewModel
            {
                MoqaysaId = m.MoqaysaId,
                JobOrderNumber = m.JobOrderNumber,
                CustomerId = m.CustomerId,
                CustomerName = m.Customer?.CustomerNameAr ?? "غير محدد",
                AccountCode = m.Customer?.Account?.AccountCode ?? "",
                RequiredQuantityText = m.RequiredQuantityText,
                RawMaterials = m.RawMaterials,
                TotalMaterials = m.TotalMaterials,
                TotalWorkerWages = m.TotalWorkerWages,
                TotalMachineCosts = m.TotalMachineCosts,
                SubTotal1 = m.SubTotal1,
                SubTotal2 = m.SubTotal2,
                SubTotal3 = m.SubTotal3,
                FinalGrandTotal = m.FinalGrandTotal,
                ApplyVat = m.ApplyVat,
                VatAmount = m.VatAmount,
                IsIncludedInTasweya = m.IsIncludedInTasweya,
                IsPaid = m.IsPaid,
                IsActive = m.IsActive,
                CreatedAtUtc = m.CreatedAtUtc,
                AccountShares = shareVms,
                PayingAccountsSummaryText = summaryText
            };
        }).ToList();
    }

    public async Task<MoqaysaDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var m = await _context.Moqaysat
            .IgnoreQueryFilters()
            .Include(x => x.Customer)
                .ThenInclude(c => c!.Account)
            .Include(x => x.MaterialItems)
            .Include(x => x.MachineryItems)
            .Include(x => x.AccountShares)
                .ThenInclude(s => s.Account)
            .FirstOrDefaultAsync(x => x.MoqaysaId == id, ct);

        return m == null ? null : MapToDetailViewModel(m);
    }

    public async Task<MoqaysaDetailViewModel> CreateAsync(MoqaysaSaveDto dto, string? userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.JobOrderNumber))
        {
            dto.JobOrderNumber = $"{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
        }

        var customer = await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId, ct);
        if (customer == null)
            throw new InvalidOperationException("الجهة الطالبة غير موجودة");

        // Validate machinery maintenance status (MED-11)
        await ValidateMachineriesOperationalAsync(dto.MachineryItems, ct);

        var calc = await PerformCalculationInternalAsync(dto, ct);

        var moqaysa = new Moqaysa
        {
            JobOrderNumber = dto.JobOrderNumber.Trim(),
            CustomerId = dto.CustomerId,
            RequiredQuantityText = dto.RequiredQuantityText?.Trim() ?? "",
            SpecificationsText = dto.SpecificationsText?.Trim() ?? "",

            RawMaterials = calc.RawMaterials,
            SelectedMaterialProcessingPercent = calc.MaterialProcessingPercent,
            MaterialProcessingFee = calc.MaterialProcessingFee,
            FilmsProcessingFee = calc.FilmsProcessingFee,
            TotalMaterials = calc.TotalMaterials,
            TotalWorkerWages = calc.TotalWorkerWages,
            TotalMachineCosts = calc.TotalMachineCosts,
            SubTotal1 = calc.SubTotal1,
            ApplyAdminExpenses = calc.ApplyAdminExpenses,
            SelectedAdminExpensesPercent = calc.AdminExpensesPercent,
            AdminExpenses = calc.AdminExpenses,
            TotalExpenses = calc.TotalExpenses,
            ApplyMarkup = calc.ApplyMarkup,
            SelectedMarkupPercent = calc.SelectedMarkupPercent,
            MarkupAmount = calc.MarkupAmount,
            TotalAfterMarkup = calc.TotalAfterMarkup,
            SelectedMinistryOfFinancePercent = calc.MinistryOfFinancePercent,
            MinistryOfFinanceFee = calc.MinistryOfFinanceFee,
            SelectedWorkersFundPercent = calc.WorkersFundPercent,
            WorkersSyndicateFund = calc.WorkersSyndicateFund,
            SubTotal2 = calc.SubTotal2,
            ApplyOther = calc.ApplyOther,
            SelectedOtherPercent = calc.OtherPercent,
            OtherAmount = calc.OtherAmount,
            SubTotal3 = calc.SubTotal3,
            ApplyVat = calc.ApplyVat,
            SelectedVatPercent = calc.VatPercent,
            VatAmount = calc.VatAmount,
            FinalGrandTotal = calc.FinalGrandTotal,

            IsIncludedInTasweya = false,
            IsPaid = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId,
            IsActive = true
        };

        foreach (var mat in dto.MaterialItems)
        {
            decimal total = MoqaysaCalculationService.Round2(mat.Quantity * mat.UnitPrice);
            moqaysa.MaterialItems.Add(new MoqaysaMaterialItem
            {
                MaterialId = mat.MaterialId,
                MaterialNameSnapshot = mat.MaterialNameSnapshot?.Trim() ?? "خامة",
                Quantity = mat.Quantity,
                UnitPrice = mat.UnitPrice,
                TotalPrice = total
            });
        }

        foreach (var mac in dto.MachineryItems)
        {
            decimal workerHours = mac.Hours;
            decimal machineHours = mac.MachineHours ?? mac.Hours;
            decimal wCost = MoqaysaCalculationService.Round2(workerHours * mac.WorkerHourlyRateSnapshot);
            decimal mCost = MoqaysaCalculationService.Round2(machineHours * mac.MachineHourlyRateSnapshot);

            moqaysa.MachineryItems.Add(new MoqaysaMachineryItem
            {
                MachineryId = mac.MachineryId,
                MachineryNameSnapshot = mac.MachineryNameSnapshot?.Trim() ?? "آلة",
                Hours = workerHours,
                WorkerHourlyRateSnapshot = mac.WorkerHourlyRateSnapshot,
                TotalWorkerWages = wCost,
                MachineHourlyRateSnapshot = mac.MachineHourlyRateSnapshot,
                TotalMachineOperatingCost = mCost
            });
        }

        _context.Moqaysat.Add(moqaysa);
        await _context.SaveChangesAsync(ct);

        // Handle AccountShares
        if (dto.AccountShares != null && dto.AccountShares.Count > 0)
        {
            decimal totalPct = dto.AccountShares.Sum(s => s.SharePercentage);
            if (Math.Abs(totalPct - 100m) > 0.01m)
                throw new InvalidOperationException($"مجموع نسب الحسابات الدافعة يجب أن يساوي 100%. المجموع الحالي: {totalPct}%");

            foreach (var share in dto.AccountShares)
            {
                moqaysa.AccountShares.Add(new MoqaysaAccountShare
                {
                    MoqaysaId = moqaysa.MoqaysaId,
                    AccountId = share.AccountId,
                    SharePercentage = share.SharePercentage,
                    AllocatedAmount = MoqaysaCalculationService.Round2(moqaysa.FinalGrandTotal * (share.SharePercentage / 100m))
                });
            }
        }
        else
        {
            // Ensure customer account is loaded
            if (customer.Account == null)
                await _context.Entry(customer).Reference(c => c.Account).LoadAsync(ct);

            int defaultAccountId = customer.AccountId;
            moqaysa.AccountShares.Add(new MoqaysaAccountShare
            {
                MoqaysaId = moqaysa.MoqaysaId,
                AccountId = defaultAccountId,
                SharePercentage = 100.00m,
                AllocatedAmount = moqaysa.FinalGrandTotal
            });
        }

        await _context.SaveChangesAsync(ct);

        await _context.Entry(moqaysa).Reference(m => m.Customer).Query().Include(c => c.Account).LoadAsync(ct);
        await _context.Entry(moqaysa).Collection(m => m.AccountShares).Query().Include(s => s.Account).LoadAsync(ct);
        return MapToDetailViewModel(moqaysa);
    }

    public async Task<MoqaysaDetailViewModel?> UpdateAsync(int id, MoqaysaSaveDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var moqaysa = await _context.Moqaysat
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .Include(m => m.AccountShares)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id, ct);

        if (moqaysa == null) return null;

        if (moqaysa.IsIncludedInTasweya)
            throw new InvalidOperationException("لا يمكن تعديل المقايسة لأنها مُدرجة بالفعل في كشف تسوية / مطالبة رسمية");

        // Validate machinery maintenance status (MED-11)
        await ValidateMachineriesOperationalAsync(dto.MachineryItems, ct);

        var calc = await PerformCalculationInternalAsync(dto, ct);

        if (!string.IsNullOrWhiteSpace(dto.JobOrderNumber))
        {
            moqaysa.JobOrderNumber = dto.JobOrderNumber.Trim();
        }
        moqaysa.CustomerId = dto.CustomerId;
        moqaysa.RequiredQuantityText = dto.RequiredQuantityText?.Trim() ?? "";
        moqaysa.SpecificationsText = dto.SpecificationsText?.Trim() ?? "";

        moqaysa.RawMaterials = calc.RawMaterials;
        moqaysa.SelectedMaterialProcessingPercent = calc.MaterialProcessingPercent;
        moqaysa.MaterialProcessingFee = calc.MaterialProcessingFee;
        moqaysa.FilmsProcessingFee = calc.FilmsProcessingFee;
        moqaysa.TotalMaterials = calc.TotalMaterials;
        moqaysa.TotalWorkerWages = calc.TotalWorkerWages;
        moqaysa.TotalMachineCosts = calc.TotalMachineCosts;
        moqaysa.SubTotal1 = calc.SubTotal1;
        moqaysa.ApplyAdminExpenses = calc.ApplyAdminExpenses;
        moqaysa.SelectedAdminExpensesPercent = calc.AdminExpensesPercent;
        moqaysa.AdminExpenses = calc.AdminExpenses;
        moqaysa.TotalExpenses = calc.TotalExpenses;
        moqaysa.ApplyMarkup = calc.ApplyMarkup;
        moqaysa.SelectedMarkupPercent = calc.SelectedMarkupPercent;
        moqaysa.MarkupAmount = calc.MarkupAmount;
        moqaysa.TotalAfterMarkup = calc.TotalAfterMarkup;
        moqaysa.SelectedMinistryOfFinancePercent = calc.MinistryOfFinancePercent;
        moqaysa.MinistryOfFinanceFee = calc.MinistryOfFinanceFee;
        moqaysa.SelectedWorkersFundPercent = calc.WorkersFundPercent;
        moqaysa.WorkersSyndicateFund = calc.WorkersSyndicateFund;
        moqaysa.SubTotal2 = calc.SubTotal2;
        moqaysa.ApplyOther = calc.ApplyOther;
        moqaysa.SelectedOtherPercent = calc.OtherPercent;
        moqaysa.OtherAmount = calc.OtherAmount;
        moqaysa.SubTotal3 = calc.SubTotal3;
        moqaysa.ApplyVat = calc.ApplyVat;
        moqaysa.SelectedVatPercent = calc.VatPercent;
        moqaysa.VatAmount = calc.VatAmount;
        moqaysa.FinalGrandTotal = calc.FinalGrandTotal;

        _context.MoqaysaMaterialItems.RemoveRange(moqaysa.MaterialItems);
        _context.MoqaysaMachineryItems.RemoveRange(moqaysa.MachineryItems);
        _context.MoqaysaAccountShares.RemoveRange(moqaysa.AccountShares);

        foreach (var mat in dto.MaterialItems)
        {
            decimal total = MoqaysaCalculationService.Round2(mat.Quantity * mat.UnitPrice);
            moqaysa.MaterialItems.Add(new MoqaysaMaterialItem
            {
                MaterialId = mat.MaterialId,
                MaterialNameSnapshot = mat.MaterialNameSnapshot?.Trim() ?? "خامة",
                Quantity = mat.Quantity,
                UnitPrice = mat.UnitPrice,
                TotalPrice = total
            });
        }

        foreach (var mac in dto.MachineryItems)
        {
            decimal workerHours = mac.Hours;
            decimal machineHours = mac.MachineHours ?? mac.Hours;
            decimal wCost = MoqaysaCalculationService.Round2(workerHours * mac.WorkerHourlyRateSnapshot);
            decimal mCost = MoqaysaCalculationService.Round2(machineHours * mac.MachineHourlyRateSnapshot);

            moqaysa.MachineryItems.Add(new MoqaysaMachineryItem
            {
                MachineryId = mac.MachineryId,
                MachineryNameSnapshot = mac.MachineryNameSnapshot?.Trim() ?? "آلة",
                Hours = workerHours,
                WorkerHourlyRateSnapshot = mac.WorkerHourlyRateSnapshot,
                TotalWorkerWages = wCost,
                MachineHourlyRateSnapshot = mac.MachineHourlyRateSnapshot,
                TotalMachineOperatingCost = mCost
            });
        }

        await _context.SaveChangesAsync(ct);

        // Handle AccountShares on update
        if (dto.AccountShares != null && dto.AccountShares.Count > 0)
        {
            decimal totalPct = dto.AccountShares.Sum(s => s.SharePercentage);
            if (Math.Abs(totalPct - 100m) > 0.01m)
                throw new InvalidOperationException($"مجموع نسب الحسابات الدافعة يجب أن يساوي 100%. المجموع الحالي: {totalPct}%");

            foreach (var share in dto.AccountShares)
            {
                moqaysa.AccountShares.Add(new MoqaysaAccountShare
                {
                    MoqaysaId = moqaysa.MoqaysaId,
                    AccountId = share.AccountId,
                    SharePercentage = share.SharePercentage,
                    AllocatedAmount = MoqaysaCalculationService.Round2(moqaysa.FinalGrandTotal * (share.SharePercentage / 100m))
                });
            }
        }
        else
        {
            int defaultAccountId = moqaysa.Customer?.AccountId ?? 0;
            if (defaultAccountId == 0)
            {
                var cust = await _context.Customers.FindAsync(new object[] { moqaysa.CustomerId }, ct);
                defaultAccountId = cust?.AccountId ?? 0;
            }

            if (defaultAccountId > 0)
            {
                moqaysa.AccountShares.Add(new MoqaysaAccountShare
                {
                    MoqaysaId = moqaysa.MoqaysaId,
                    AccountId = defaultAccountId,
                    SharePercentage = 100.00m,
                    AllocatedAmount = moqaysa.FinalGrandTotal
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        // Reload AccountShares with Account navigation for mapping
        await _context.Entry(moqaysa).Collection(m => m.AccountShares).Query().Include(s => s.Account).LoadAsync(ct);
        return MapToDetailViewModel(moqaysa);
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Moqaysat.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.MoqaysaId == id, ct);
        if (item == null) return false;

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync(ct);
        return item.IsActive;
    }

    public async Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id, ct);

        return item == null ? null : _wordExportService.ExportMoqaysaToWord(item);
    }

    public async Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id, ct);

        return item == null ? null : _pdfExportService.ExportMoqaysaToPdf(item);
    }

    private async Task<MoqaysaCalculationResult> PerformCalculationInternalAsync(MoqaysaSaveDto dto, CancellationToken ct)
    {
        decimal rawMaterialsSum = dto.MaterialItems.Sum(m => MoqaysaCalculationService.Round2(m.Quantity * m.UnitPrice));
        decimal totalWorkerWages = dto.MachineryItems.Sum(m => MoqaysaCalculationService.Round2(m.Hours * m.WorkerHourlyRateSnapshot));
        decimal totalMachineCosts = dto.MachineryItems.Sum(m =>
        {
            decimal machineHours = m.MachineHours ?? m.Hours;
            return MoqaysaCalculationService.Round2(machineHours * m.MachineHourlyRateSnapshot);
        });

        var configs = await _context.CostingFormulaConfigs.Where(c => c.IsActive).ToListAsync(ct);
        var matProcCfg = configs.FirstOrDefault(c => c.FieldKey == "MATERIAL_PROCESSING");
        var adminExpCfg = configs.FirstOrDefault(c => c.FieldKey == "ADMIN_EXPENSES");
        var mofCfg = configs.FirstOrDefault(c => c.FieldKey == "MINISTRY_OF_FINANCE");
        var workersCfg = configs.FirstOrDefault(c => c.FieldKey == "WORKERS_FUND");
        var otherCfg = configs.FirstOrDefault(c => c.FieldKey == "OTHER");
        var vatCfg = configs.FirstOrDefault(c => c.FieldKey == "VAT");

        decimal parsedQuantity = ParseQuantity(dto.RequiredQuantityText);

        var input = new MoqaysaCalculationInput
        {
            RawMaterials = rawMaterialsSum,
            FilmsProcessingFee = dto.FilmsProcessingFee,
            TotalWorkerWages = totalWorkerWages,
            TotalMachineCosts = totalMachineCosts,

            ApplyAdminExpenses = dto.ApplyAdminExpenses,
            ApplyMarkup = dto.ApplyMarkup,
            SelectedMarkupPercent = dto.SelectedMarkupPercent,
            ApplyOther = dto.ApplyOther,
            ApplyVat = dto.ApplyVat,

            MaterialProcessingPercent = dto.SelectedMaterialProcessingPercent ?? matProcCfg?.DefaultPercentage ?? 5.5m,
            AdminExpensesPercent = dto.SelectedAdminExpensesPercent ?? adminExpCfg?.DefaultPercentage ?? 8.5m,
            MinistryOfFinancePercent = dto.SelectedMinistryOfFinancePercent ?? mofCfg?.DefaultPercentage ?? 15.0m,
            WorkersFundPercent = dto.SelectedWorkersFundPercent ?? workersCfg?.DefaultPercentage ?? 10.0m,
            WorkersFundBaseMode = workersCfg?.WorkersFundBaseMode ?? "DiffTotalMinusMaterials",
            OtherPercent = dto.SelectedOtherPercent ?? otherCfg?.DefaultPercentage ?? 10.0m,
            VatPercent = dto.SelectedVatPercent ?? vatCfg?.DefaultPercentage ?? 14.0m,

            Quantity = parsedQuantity
        };

        return _calculationService.Calculate(input);
    }

    private async Task ValidateMachineriesOperationalAsync(List<MachineryItemInputDto> machineryItems, CancellationToken ct)
    {
        var scheduled = machineryItems
            .Where(m => m.Hours > 0m || (m.MachineHours.HasValue && m.MachineHours.Value > 0m))
            .ToList();

        if (scheduled.Count == 0) return;

        var macIds = scheduled.Select(m => m.MachineryId).Distinct().ToList();
        var machineries = await _context.Machineries
            .Where(m => macIds.Contains(m.MachineryId))
            .ToListAsync(ct);

        foreach (var item in scheduled)
        {
            var mac = machineries.FirstOrDefault(m => m.MachineryId == item.MachineryId);
            if (mac != null)
            {
                if (string.Equals(mac.MaintenanceStatus, "UnderMaintenance", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mac.MaintenanceStatus, "OutOfService", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"لا يمكن جدولة ساعات تشغيل على الآلة '{mac.MachineryNameAr}' لأن حالتها الحالية: {mac.MaintenanceStatus}");
                }
            }
        }
    }

    private static MoqaysaDetailViewModel MapToDetailViewModel(Moqaysa m)
    {
        decimal parsedQuantity = ParseQuantity(m.RequiredQuantityText);
        decimal unitPriceBeforeVat = parsedQuantity > 0 ? MoqaysaCalculationService.Round2(m.SubTotal3 / parsedQuantity) : 0m;
        decimal finalUnitPrice = parsedQuantity > 0 ? MoqaysaCalculationService.Round2(m.FinalGrandTotal / parsedQuantity) : 0m;

        return new MoqaysaDetailViewModel
        {
            MoqaysaId = m.MoqaysaId,
            JobOrderNumber = m.JobOrderNumber,
            CustomerId = m.CustomerId,
            CustomerName = m.Customer?.CustomerNameAr,
            AccountCode = m.Customer?.Account?.AccountCode,
            Customer = m.Customer != null ? new CustomerViewModel
            {
                CustomerId = m.Customer.CustomerId,
                CustomerNameAr = m.Customer.CustomerNameAr,
                AccountId = m.Customer.AccountId,
                AccountCode = m.Customer.Account?.AccountCode,
                AccountNameAr = m.Customer.Account?.AccountNameAr ?? string.Empty,
                DebitHeaderTextAr = m.Customer.Account?.DebitHeaderTextAr,
                Phone = m.Customer.Phone,
                Email = m.Customer.Email,
                Address = m.Customer.Address,
                Notes = m.Customer.Notes,
                IsActive = m.Customer.IsActive
            } : null,
            RequiredQuantityText = m.RequiredQuantityText,
            SpecificationsText = m.SpecificationsText,
            RawMaterials = m.RawMaterials,
            MaterialProcessingPercent = m.SelectedMaterialProcessingPercent,
            MaterialProcessingFee = m.MaterialProcessingFee,
            FilmsProcessingFee = m.FilmsProcessingFee,
            TotalMaterials = m.TotalMaterials,
            TotalWorkerWages = m.TotalWorkerWages,
            TotalMachineCosts = m.TotalMachineCosts,
            SubTotal1 = m.SubTotal1,
            ApplyAdminExpenses = m.ApplyAdminExpenses,
            AdminExpensesPercent = m.SelectedAdminExpensesPercent,
            AdminExpenses = m.AdminExpenses,
            TotalExpenses = m.TotalExpenses,
            ApplyMarkup = m.ApplyMarkup,
            SelectedMarkupPercent = m.SelectedMarkupPercent,
            MarkupAmount = m.MarkupAmount,
            TotalAfterMarkup = m.TotalAfterMarkup,
            MinistryOfFinancePercent = m.SelectedMinistryOfFinancePercent,
            MinistryOfFinanceFee = m.MinistryOfFinanceFee,
            WorkersFundPercent = m.SelectedWorkersFundPercent,
            WorkersSyndicateFund = m.WorkersSyndicateFund,
            SubTotal2 = m.SubTotal2,
            ApplyOther = m.ApplyOther,
            OtherPercent = m.SelectedOtherPercent,
            OtherAmount = m.OtherAmount,
            SubTotal3 = m.SubTotal3,
            ApplyVat = m.ApplyVat,
            VatPercent = m.SelectedVatPercent,
            VatAmount = m.VatAmount,
            FinalGrandTotal = m.FinalGrandTotal,
            ParsedQuantity = parsedQuantity,
            UnitPriceBeforeVat = unitPriceBeforeVat,
            FinalUnitPrice = finalUnitPrice,
            IsIncludedInTasweya = m.IsIncludedInTasweya,
            IsPaid = m.IsPaid,
            IsActive = m.IsActive,
            CreatedAtUtc = m.CreatedAtUtc,
            CreatedByUserId = m.CreatedByUserId,
            MaterialItems = m.MaterialItems.Select(mat => new MoqaysaMaterialItemViewModel
            {
                ItemId = mat.ItemId,
                MoqaysaId = mat.MoqaysaId,
                MaterialId = mat.MaterialId,
                MaterialNameSnapshot = mat.MaterialNameSnapshot,
                Quantity = mat.Quantity,
                UnitPrice = mat.UnitPrice,
                TotalPrice = mat.TotalPrice
            }).ToList(),
            MachineryItems = m.MachineryItems.Select(mac => new MoqaysaMachineryItemViewModel
            {
                ItemId = mac.OperationId,
                MoqaysaId = mac.MoqaysaId,
                MachineryId = mac.MachineryId,
                MachineryNameSnapshot = mac.MachineryNameSnapshot,
                Hours = mac.Hours,
                WorkerHourlyRateSnapshot = mac.WorkerHourlyRateSnapshot,
                TotalWorkerWages = mac.TotalWorkerWages,
                MachineHourlyRateSnapshot = mac.MachineHourlyRateSnapshot,
                TotalMachineOperatingCost = mac.TotalMachineOperatingCost
            }).ToList(),
            AccountShares = m.AccountShares.Select(s => new MoqaysaAccountShareViewModel
            {
                AccountId = s.AccountId,
                AccountNameAr = s.Account?.AccountNameAr ?? string.Empty,
                DebitHeaderTextAr = s.Account?.DebitHeaderTextAr ?? string.Empty,
                SharePercentage = s.SharePercentage,
                AllocatedAmount = s.AllocatedAmount
            }).ToList()
        };
    }
}
