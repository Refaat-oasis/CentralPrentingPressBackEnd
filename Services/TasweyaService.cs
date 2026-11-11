using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.Helpers;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class TasweyaService : ITasweyaService
{
    private readonly ApplicationDbContext _context;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;
    private readonly ITafqeetService _tafqeetService;
    private readonly Microsoft.Extensions.Logging.ILogger<TasweyaService> _logger;

    public TasweyaService(
        ApplicationDbContext context,
        WordExportService wordExportService,
        PdfExportService pdfExportService,
        ITafqeetService tafqeetService,
        Microsoft.Extensions.Logging.ILogger<TasweyaService> logger)
    {
        _context = context;
        _wordExportService = wordExportService;
        _pdfExportService = pdfExportService;
        _tafqeetService = tafqeetService;
        _logger = logger;
    }

    public async Task<List<TasweyaUnsettledMoqaysaViewModel>> GetUnsettledMoqaysatAsync(
        int? customerId,
        int? accountId,
        int? includeTasweyaId = null,
        CancellationToken ct = default)
    {
        var query = _context.Moqaysat
            .AsNoTracking()
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Where(m => m.IsActive && !m.IsPaid);

        if (includeTasweyaId.HasValue)
        {
            int targetTasweyaId = includeTasweyaId.Value;
            query = query.Where(m =>
                !m.IsIncludedInTasweya ||
                _context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.TasweyaId == targetTasweyaId) ||
                !_context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.TasweyaId != targetTasweyaId && link.Tasweya != null && link.Tasweya.IsActive));
        }
        else
        {
            query = query.Where(m =>
                !m.IsIncludedInTasweya ||
                !_context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.Tasweya != null && link.Tasweya.IsActive));
        }

        if (customerId.HasValue)
            query = query.Where(m => m.CustomerId == customerId.Value);

        if (accountId.HasValue)
            query = query.Where(m => m.Customer != null && m.Customer.AccountId == accountId.Value);

        return await query
            .OrderByDescending(m => m.MoqaysaId)
            .Select(m => new TasweyaUnsettledMoqaysaViewModel
            {
                MoqaysaId = m.MoqaysaId,
                JobOrderNumber = m.JobOrderNumber,
                CustomerId = m.CustomerId,
                CustomerNameAr = m.Customer != null ? m.Customer.CustomerNameAr : "غير محدد",
                AccountId = m.Customer != null ? m.Customer.AccountId : 0,
                AccountNameAr = m.Customer != null && m.Customer.Account != null ? m.Customer.Account.AccountNameAr : "",
                RequiredQuantityText = m.RequiredQuantityText,
                RawMaterials = m.RawMaterials,
                MaterialProcessingFee = m.MaterialProcessingFee,
                FilmsProcessingFee = m.FilmsProcessingFee,
                TotalMaterials = m.TotalMaterials,
                TotalWorkerWages = m.TotalWorkerWages,
                TotalMachineCosts = m.TotalMachineCosts,
                AdminExpenses = m.AdminExpenses,
                MarkupAmount = m.MarkupAmount,
                ApplyMarkup = m.ApplyMarkup,
                SelectedMarkupPercent = m.SelectedMarkupPercent,
                MinistryOfFinanceFee = m.MinistryOfFinanceFee,
                WorkersSyndicateFund = m.WorkersSyndicateFund,
                OtherAmount = m.OtherAmount,
                ApplyVat = m.ApplyVat,
                VatAmount = m.VatAmount,
                FinalGrandTotal = m.FinalGrandTotal,
                CreatedAtUtc = m.CreatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<List<TasweyaSummaryViewModel>> GetAllAsync(
        int? accountId,
        int? customerId,
        bool? isPaid,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.Tasweyat
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .Include(t => t.PaymentOrderLinks.Where(l => l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive))
                .ThenInclude(l => l.PaymentOrder)
            .AsQueryable();

        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (customerId.HasValue) query = query.Where(t => t.CustomerId == customerId.Value);
        if (isPaid.HasValue) query = query.Where(t => t.IsPaid == isPaid.Value);
        if (activeOnly) query = query.Where(t => t.IsActive);

        var items = await query
            .OrderByDescending(t => t.TasweyaId)
            .ToListAsync(ct);

        return items.Select(t =>
        {
            var activeLink = t.PaymentOrderLinks.FirstOrDefault(l => l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive);
            return new TasweyaSummaryViewModel
            {
                TasweyaId = t.TasweyaId,
                SettlementNumber = t.SettlementNumber,
                SettlementDate = t.SettlementDate,
                AccountId = t.AccountId,
                AccountNameAr = t.Account != null ? t.Account.AccountNameAr : "غير محدد",
                CustomerId = t.CustomerId,
                CustomerNameAr = t.Customer != null ? t.Customer.CustomerNameAr : "غير محدد",
                ClaimNumber = t.ClaimNumber,
                JobOrderNumbersText = t.JobOrderNumbersText,
                SettlementGrandTotal = t.SettlementGrandTotal,
                TotalAmountInWordsAr = t.TotalAmountInWordsAr,
                IsPaid = t.IsPaid,
                PaidAtUtc = t.PaidAtUtc,
                IsActive = t.IsActive,
                ActivePaymentOrderId = activeLink?.PaymentOrderId,
                ActivePaymentOrderNumber = activeLink?.PaymentOrder?.PaymentOrderNumber,
                CreatedAtUtc = t.CreatedAtUtc
            };
        }).ToList();
    }

    public async Task<TasweyaDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _context.Tasweyat
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Customer)
            .Include(x => x.MoqaysaLinks)
                .ThenInclude(l => l.Moqaysa)
            .Include(x => x.PaymentOrderLinks.Where(l => l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive))
                .ThenInclude(l => l.PaymentOrder)
            .FirstOrDefaultAsync(x => x.TasweyaId == id, ct);

        if (t == null) return null;

        var activeLink = t.PaymentOrderLinks.FirstOrDefault(l => l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive);

        return new TasweyaDetailViewModel
        {
            TasweyaId = t.TasweyaId,
            SettlementNumber = t.SettlementNumber,
            SettlementDate = t.SettlementDate,
            AccountId = t.AccountId,
            Account = t.Account != null ? new AccountViewModel
            {
                AccountId = t.Account.AccountId,
                AccountNameAr = t.Account.AccountNameAr,
                AccountCode = t.Account.AccountCode,
                DebitHeaderTextAr = t.Account.DebitHeaderTextAr,
                BudgetCeiling = t.Account.BudgetCeiling,
                IsActive = t.Account.IsActive
            } : null,
            CustomerId = t.CustomerId,
            Customer = t.Customer != null ? new CustomerViewModel
            {
                CustomerId = t.Customer.CustomerId,
                CustomerNameAr = t.Customer.CustomerNameAr,
                AccountId = t.Customer.AccountId,
                BudgetCeiling = t.Customer.BudgetCeiling,
                Phone = t.Customer.Phone,
                Email = t.Customer.Email,
                Address = t.Customer.Address,
                Notes = t.Customer.Notes,
                IsActive = t.Customer.IsActive
            } : null,
            ClaimNumber = t.ClaimNumber,
            JobOrderNumbersText = t.JobOrderNumbersText,
            TotalMaterialsSum = t.TotalMaterialsSum,
            TotalWorkerWagesSum = t.TotalWorkerWagesSum,
            TotalMachineCostsSum = t.TotalMachineCostsSum,
            AdminExpensesSum = t.AdminExpensesSum,
            MarkupAmountSum = t.MarkupAmountSum,
            MinistryOfFinanceSum = t.MinistryOfFinanceSum,
            WorkersFundSum = t.WorkersFundSum,
            OtherAmountSum = t.OtherAmountSum,
            RevenuesTotal = t.RevenuesTotal,
            VatTotal = t.VatTotal,
            SettlementGrandTotal = t.SettlementGrandTotal,
            TotalAmountInWordsAr = t.TotalAmountInWordsAr,
            IsPaid = t.IsPaid,
            PaidAtUtc = t.PaidAtUtc,
            IsActive = t.IsActive,
            ActivePaymentOrderId = activeLink?.PaymentOrderId,
            ActivePaymentOrderNumber = activeLink?.PaymentOrder?.PaymentOrderNumber,
            CreatedAtUtc = t.CreatedAtUtc,
            MoqaysaLinks = t.MoqaysaLinks.Select(ml => new TasweyaMoqaysaItemViewModel
            {
                LinkId = ml.LinkId,
                MoqaysaId = ml.MoqaysaId,
                JobOrderNumber = ml.Moqaysa?.JobOrderNumber ?? "",
                FinalGrandTotal = ml.Moqaysa?.FinalGrandTotal ?? 0m,
                ApplyVat = ml.Moqaysa?.ApplyVat ?? false,
                VatAmount = ml.Moqaysa?.VatAmount ?? 0m
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message, TasweyaCreateResultViewModel? Result)> CreateAsync(
        CreateTasweyaDto dto,
        string? userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.SettlementNumber))
            return (false, "يرجى كتابة رقم كشف التسوية", null);

        // Format settlement number with financial year if entered as bare number (e.g., "55" -> "55 / 2026")
        var finalSettlementNumber = FinancialYearHelper.FormatSettlementNumber(dto.SettlementNumber, dto.SettlementDate);
        var fy = FinancialYearHelper.GetFinancialYear(dto.SettlementDate);

        // Uniqueness is enforced within the same financial year (annual cycle restarts May 1st)
        var existsInFy = await _context.Tasweyat
            .IgnoreQueryFilters()
            .AnyAsync(t => t.SettlementNumber == finalSettlementNumber &&
                           t.SettlementDate >= fy.StartDate &&
                           t.SettlementDate <= fy.EndDate, ct);
        if (existsInFy)
            return (false, $"رقم كشف التسوية '{finalSettlementNumber}' مسجل بالفعل ضمن السنة المالية ({fy.Code}). يرجى استخدام رقم آخر.", null);

        if (dto.MoqaysaIds == null || dto.MoqaysaIds.Count == 0)
            return (false, "يرجى تحديد مقايسة واحدة على الأقل لإدراجها في كشف التسوية", null);

        var account = await _context.Accounts.FindAsync(new object[] { dto.AccountId }, ct);
        if (account == null)
            return (false, "الحساب المختار غير صحيح", null);

        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, ct);
        if (customer == null)
            return (false, "الجهة الطالبة غير موجودة", null);

        var moqaysat = await _context.Moqaysat
            .Where(m => dto.MoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync(ct);

        if (moqaysat.Count != dto.MoqaysaIds.Count)
            return (false, "بعض المقايسات المحددة غير موجودة أو غير فعالة", null);

        var activeConflicts = await _context.TasweyaMoqaysaLinks
            .Include(link => link.Moqaysa)
            .Where(link => dto.MoqaysaIds.Contains(link.MoqaysaId) && link.Tasweya != null && link.Tasweya.IsActive)
            .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
            .Where(jobNo => !string.IsNullOrEmpty(jobNo))
            .Distinct()
            .ToListAsync(ct);

        if (activeConflicts.Count > 0)
            return (false, $"إحدى المقايسات المحددة (إذن تشغيل: {string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط", null);

        var totals = CalculateTotals(moqaysat, dto.OverrideMarkupPercent);
        string tafqeet = _tafqeetService.ToWords(totals.SettlementGrandTotal);
        string jobOrders = string.Join(" , ", moqaysat.Select(m => m.JobOrderNumber).Distinct());

        Tasweya? createdTasweya = null;

        Action buildTasweya = () =>
        {
            var tasweya = new Tasweya
            {
                SettlementNumber = finalSettlementNumber,
                SettlementDate = dto.SettlementDate,
                AccountId = dto.AccountId,
                CustomerId = dto.CustomerId,
                ClaimNumber = string.IsNullOrWhiteSpace(dto.ClaimNumber) ? finalSettlementNumber : dto.ClaimNumber.Trim(),
                JobOrderNumbersText = jobOrders,

                TotalMaterialsSum = totals.TotalMaterialsSum,
                TotalWorkerWagesSum = totals.TotalWorkerWagesSum,
                TotalMachineCostsSum = totals.TotalMachineCostsSum,
                AdminExpensesSum = totals.AdminExpensesSum,
                MarkupAmountSum = totals.MarkupAmountSum,
                MinistryOfFinanceSum = totals.MinistryOfFinanceSum,
                WorkersFundSum = totals.WorkersFundSum,
                OtherAmountSum = totals.OtherAmountSum,

                RevenuesTotal = totals.RevenuesTotal,
                VatTotal = totals.VatTotal,
                SettlementGrandTotal = totals.SettlementGrandTotal,
                TotalAmountInWordsAr = tafqeet,

                IsPaid = false,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId,
                IsActive = true
            };

            foreach (var m in moqaysat)
            {
                m.IsIncludedInTasweya = true;
                tasweya.MoqaysaLinks.Add(new TasweyaMoqaysaLink
                {
                    MoqaysaId = m.MoqaysaId
                });
            }

            _context.Tasweyat.Add(tasweya);
            createdTasweya = tasweya;
        };

        try
        {
            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    buildTasweya();
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                buildTasweya();
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save Tasweya due to database conflict.");
            return (false, "تعذر حفظ كشف التسوية: قد يكون رقم كشف التسوية مسجلاً مسبقاً أو هناك تعارض في البيانات", null);
        }

        if (createdTasweya == null)
            return (false, "فشل إنشاء كشف التسوية", null);

        var result = new TasweyaCreateResultViewModel
        {
            Message = "تم إصدار كشف التسوية (المطالبة المالية) بنجاح",
            TasweyaId = createdTasweya.TasweyaId,
            VatTotal = createdTasweya.VatTotal,
            SettlementGrandTotal = createdTasweya.SettlementGrandTotal,
            RevenuesTotal = createdTasweya.RevenuesTotal,
            TotalMaterialsSum = createdTasweya.TotalMaterialsSum,
            TotalWorkerWagesSum = createdTasweya.TotalWorkerWagesSum,
            TotalMachineCostsSum = createdTasweya.TotalMachineCostsSum,
            TotalAmountInWordsAr = createdTasweya.TotalAmountInWordsAr
        };

        return (true, result.Message, result);
    }

    public async Task<(bool Success, string Message, int? TasweyaId)> UpdateAsync(
        int id,
        UpdateTasweyaDto dto,
        CancellationToken ct = default)
    {
        var tasweya = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
            .FirstOrDefaultAsync(t => t.TasweyaId == id, ct);

        if (tasweya == null)
            return (false, "كشف التسوية غير موجود", null);

        if (tasweya.IsPaid)
            return (false, "لا يمكن تعديل كشف تسوية تم سداده وتأكيده بأمر دفع رسمي", null);

        // CRIT-06: Check active payment order links
        bool hasActivePaymentOrder = await _context.PaymentOrderTasweyaLinks
            .AnyAsync(l => l.TasweyaId == id && l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive, ct);
        if (hasActivePaymentOrder)
            return (false, "لا يمكن تعديل كشف تسوية مرتبط بأمر دفع نشط", null);

        if (string.IsNullOrWhiteSpace(dto.SettlementNumber))
            return (false, "يرجى كتابة رقم كشف التسوية", null);

        var finalSettlementNumber = FinancialYearHelper.FormatSettlementNumber(dto.SettlementNumber, dto.SettlementDate);
        var fy = FinancialYearHelper.GetFinancialYear(dto.SettlementDate);

        var existsInFy = await _context.Tasweyat
            .IgnoreQueryFilters()
            .AnyAsync(t => t.TasweyaId != id &&
                           t.SettlementNumber == finalSettlementNumber &&
                           t.SettlementDate >= fy.StartDate &&
                           t.SettlementDate <= fy.EndDate, ct);
        if (existsInFy)
            return (false, $"رقم كشف التسوية '{finalSettlementNumber}' مسجل بالفعل في كشف تسوية آخر ضمن السنة المالية ({fy.Code}). يرجى استخدام رقم آخر.", null);

        if (dto.MoqaysaIds == null || dto.MoqaysaIds.Count == 0)
            return (false, "يرجى تحديد مقايسة واحدة على الأقل", null);

        var account = await _context.Accounts.FindAsync(new object[] { dto.AccountId }, ct);
        if (account == null)
            return (false, "الحساب المختار غير صحيح", null);

        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, ct);
        if (customer == null)
            return (false, "الجهة الطالبة غير موجودة", null);

        var moqaysat = await _context.Moqaysat
            .Where(m => dto.MoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync(ct);

        if (moqaysat.Count != dto.MoqaysaIds.Count)
            return (false, "بعض المقايسات المحددة غير موجودة أو غير فعالة", null);

        var activeConflicts = await _context.TasweyaMoqaysaLinks
            .Include(link => link.Moqaysa)
            .Where(link => dto.MoqaysaIds.Contains(link.MoqaysaId) && link.TasweyaId != id && link.Tasweya != null && link.Tasweya.IsActive)
            .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
            .Where(jobNo => !string.IsNullOrEmpty(jobNo))
            .Distinct()
            .ToListAsync(ct);

        if (activeConflicts.Count > 0)
            return (false, $"إحدى المقايسات المحددة (إذن تشغيل: {string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط", null);

        var totals = CalculateTotals(moqaysat, dto.OverrideMarkupPercent);
        string tafqeet = _tafqeetService.ToWords(totals.SettlementGrandTotal);
        string jobOrders = string.Join(" , ", moqaysat.Select(m => m.JobOrderNumber).Distinct());

        Action applyUpdate = () =>
        {
            // Release old moqaysat
            var oldMoqaysaIds = tasweya.MoqaysaLinks.Select(ml => ml.MoqaysaId).ToList();
            var oldMoqaysat = _context.Moqaysat.Where(m => oldMoqaysaIds.Contains(m.MoqaysaId)).ToList();
            foreach (var om in oldMoqaysat)
            {
                om.IsIncludedInTasweya = false;
            }
            _context.TasweyaMoqaysaLinks.RemoveRange(tasweya.MoqaysaLinks);

            tasweya.SettlementNumber = finalSettlementNumber;
            tasweya.SettlementDate = dto.SettlementDate;
            tasweya.AccountId = dto.AccountId;
            tasweya.CustomerId = dto.CustomerId;
            tasweya.ClaimNumber = string.IsNullOrWhiteSpace(dto.ClaimNumber) ? finalSettlementNumber : dto.ClaimNumber.Trim();
            tasweya.JobOrderNumbersText = jobOrders;

            tasweya.TotalMaterialsSum = totals.TotalMaterialsSum;
            tasweya.TotalWorkerWagesSum = totals.TotalWorkerWagesSum;
            tasweya.TotalMachineCostsSum = totals.TotalMachineCostsSum;
            tasweya.AdminExpensesSum = totals.AdminExpensesSum;
            tasweya.MarkupAmountSum = totals.MarkupAmountSum;
            tasweya.MinistryOfFinanceSum = totals.MinistryOfFinanceSum;
            tasweya.WorkersFundSum = totals.WorkersFundSum;
            tasweya.OtherAmountSum = totals.OtherAmountSum;

            tasweya.RevenuesTotal = totals.RevenuesTotal;
            tasweya.VatTotal = totals.VatTotal;
            tasweya.SettlementGrandTotal = totals.SettlementGrandTotal;
            tasweya.TotalAmountInWordsAr = tafqeet;

            foreach (var m in moqaysat)
            {
                m.IsIncludedInTasweya = true;
                tasweya.MoqaysaLinks.Add(new TasweyaMoqaysaLink
                {
                    TasweyaId = tasweya.TasweyaId,
                    MoqaysaId = m.MoqaysaId
                });
            }
        };

        try
        {
            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    applyUpdate();
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                applyUpdate();
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update Tasweya due to database conflict.");
            return (false, "تعذر تحديث كشف التسوية: قد يكون رقم كشف التسوية مسجلاً مسبقاً أو هناك تعارض في البيانات", null);
        }

        return (true, "تم تعديل كشف التسوية وحفظ التحديثات بنجاح", tasweya.TasweyaId);
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleActiveAsync(
        int id,
        CancellationToken ct = default)
    {
        var item = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
            .FirstOrDefaultAsync(t => t.TasweyaId == id, ct);

        if (item == null)
            return (false, "كشف التسوية غير موجود", null);

        var moqaysaIds = item.MoqaysaLinks.Select(ml => ml.MoqaysaId).ToList();
        var linkedMoqaysat = await _context.Moqaysat.Where(m => moqaysaIds.Contains(m.MoqaysaId)).ToListAsync(ct);

        if (item.IsActive)
        {
            if (item.IsPaid)
                return (false, "لا يمكن إلغاء تفعيل كشف تسوية تم سداده وتأكيده بأمر دفع رسمي", null);

            // CRIT-06: Check active payment order
            bool hasActivePaymentOrder = await _context.PaymentOrderTasweyaLinks
                .AnyAsync(l => l.TasweyaId == id && l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive, ct);
            if (hasActivePaymentOrder)
                return (false, "لا يمكن إلغاء تفعيل كشف تسوية مرتبط بأمر دفع نشط", null);

            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                    foreach (var m in linkedMoqaysat)
                    {
                        m.IsIncludedInTasweya = false;
                    }

                    item.IsActive = false;
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                foreach (var m in linkedMoqaysat)
                {
                    m.IsIncludedInTasweya = false;
                }

                item.IsActive = false;
                await _context.SaveChangesAsync(ct);
            }

            return (true, "تم إلغاء تفعيل كشف التسوية وتحرير المقايسات التابعة له لتصبح متاحة في كشوف التسوية الجديدة", false);
        }
        else
        {
            var alreadyIncludedInActive = await _context.TasweyaMoqaysaLinks
                .Include(link => link.Moqaysa)
                .Where(link => moqaysaIds.Contains(link.MoqaysaId) && link.TasweyaId != item.TasweyaId && link.Tasweya != null && link.Tasweya.IsActive)
                .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
                .Where(jobNo => !string.IsNullOrEmpty(jobNo))
                .Distinct()
                .ToListAsync(ct);

            if (alreadyIncludedInActive.Count > 0)
            {
                return (false, $"لا يمكن إعادة تفعيل كشف التسوية؛ نظراً لأن المقايسات ذات أذون التشغيل ({string.Join(" ، ", alreadyIncludedInActive)}) تم إدراجها بالفعل في كشف تسوية آخر نشط.", null);
            }

            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                    foreach (var m in linkedMoqaysat)
                    {
                        m.IsIncludedInTasweya = true;
                    }

                    item.IsActive = true;
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                foreach (var m in linkedMoqaysat)
                {
                    m.IsIncludedInTasweya = true;
                }

                item.IsActive = true;
                await _context.SaveChangesAsync(ct);
            }

            return (true, "تم إعادة تفعيل كشف التسوية وربط المقايسات التابعة له بنجاح", true);
        }
    }

    public async Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Tasweyat
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TasweyaId == id, ct);

        if (item == null) return null;

        return _wordExportService.ExportTasweyaToWord(item);
    }

    public async Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.Tasweyat
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TasweyaId == id, ct);

        if (item == null) return null;

        return _pdfExportService.ExportTasweyaToPdf(item);
    }

    private static TasweyaCalculatedTotals CalculateTotals(List<Moqaysa> moqaysat, decimal? overrideMarkupPercent)
    {
        if (overrideMarkupPercent.HasValue && overrideMarkupPercent.Value >= 0)
        {
            decimal overridePercent = overrideMarkupPercent.Value;
            decimal matSum = 0m, wagesSum = 0m, machSum = 0m, adminSum = 0m;
            decimal markupSum = 0m, mofSum = 0m, workersSum = 0m, otherSum = 0m;
            decimal vatSum = 0m, revenueSum = 0m;

            foreach (var m in moqaysat)
            {
                decimal mat = m.TotalMaterials;
                decimal wages = m.TotalWorkerWages;
                decimal mach = m.TotalMachineCosts;
                decimal admin = m.AdminExpenses;
                decimal baseExp = mat + wages + mach + admin;

                decimal markup = MoqaysaCalculationService.Round4(baseExp * (overridePercent / 100m));
                decimal afterMarkup = MoqaysaCalculationService.Round4(baseExp + markup);

                decimal diffTaxes = Math.Max(0m, afterMarkup - mat);
                decimal mof = MoqaysaCalculationService.Round4(diffTaxes * 0.15m);
                decimal workers = MoqaysaCalculationService.Round4(diffTaxes * 0.10m);

                decimal subTotal2 = MoqaysaCalculationService.Round4(afterMarkup + mof + workers);
                decimal other = m.ApplyOther
                    ? MoqaysaCalculationService.Round4(subTotal2 * 0.10m)
                    : 0m;

                decimal subTotal3 = MoqaysaCalculationService.Round4(subTotal2 + other);
                // CRIT-05: VAT is strictly per-Moqaysa ApplyVat!
                decimal vat = m.ApplyVat
                    ? MoqaysaCalculationService.Round4(subTotal3 * 0.14m)
                    : 0m;

                matSum += mat;
                wagesSum += wages;
                machSum += mach;
                adminSum += admin;
                markupSum += markup;
                mofSum += mof;
                workersSum += workers;
                otherSum += other;
                revenueSum += subTotal3;
                vatSum += vat;
            }

            decimal revTotal = MoqaysaCalculationService.Round2(revenueSum);
            decimal vatTotal = MoqaysaCalculationService.Round2(vatSum);
            decimal grandTotal = MoqaysaCalculationService.Round2(revTotal + vatTotal);

            return new TasweyaCalculatedTotals
            {
                TotalMaterialsSum = MoqaysaCalculationService.Round2(matSum),
                TotalWorkerWagesSum = MoqaysaCalculationService.Round2(wagesSum),
                TotalMachineCostsSum = MoqaysaCalculationService.Round2(machSum),
                AdminExpensesSum = MoqaysaCalculationService.Round2(adminSum),
                MarkupAmountSum = MoqaysaCalculationService.Round2(markupSum),
                MinistryOfFinanceSum = MoqaysaCalculationService.Round2(mofSum),
                WorkersFundSum = MoqaysaCalculationService.Round2(workersSum),
                OtherAmountSum = MoqaysaCalculationService.Round2(otherSum),
                RevenuesTotal = revTotal,
                VatTotal = vatTotal,
                SettlementGrandTotal = grandTotal
            };
        }
        else
        {
            decimal totalMat = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.TotalMaterials));
            decimal totalWages = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.TotalWorkerWages));
            decimal totalMach = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.TotalMachineCosts));
            decimal totalAdmin = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.AdminExpenses));
            decimal totalMarkup = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.MarkupAmount));
            decimal totalMof = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.MinistryOfFinanceFee));
            decimal totalWorkers = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.WorkersSyndicateFund));
            decimal totalOther = MoqaysaCalculationService.Round2(moqaysat.Sum(m => m.OtherAmount));

            decimal revenuesTotal = MoqaysaCalculationService.Round2(
                totalMat + totalWages + totalMach + totalAdmin + totalMarkup + totalMof + totalWorkers + totalOther);

            // CRIT-05: Strictly per-Moqaysa ApplyVat check instead of global blanket check!
            decimal vatTotal = MoqaysaCalculationService.Round2(
                moqaysat.Where(m => m.ApplyVat).Sum(m => m.VatAmount));
            decimal grandTotal = MoqaysaCalculationService.Round2(revenuesTotal + vatTotal);

            return new TasweyaCalculatedTotals
            {
                TotalMaterialsSum = totalMat,
                TotalWorkerWagesSum = totalWages,
                TotalMachineCostsSum = totalMach,
                AdminExpensesSum = totalAdmin,
                MarkupAmountSum = totalMarkup,
                MinistryOfFinanceSum = totalMof,
                WorkersFundSum = totalWorkers,
                OtherAmountSum = totalOther,
                RevenuesTotal = revenuesTotal,
                VatTotal = vatTotal,
                SettlementGrandTotal = grandTotal
            };
        }
    }
}
