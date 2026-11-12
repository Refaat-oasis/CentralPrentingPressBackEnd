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
            .Include(m => m.AccountShares)
                .ThenInclude(s => s.Account)
            .Where(m => m.IsActive && !m.IsPaid);

        if (customerId.HasValue)
            query = query.Where(m => m.CustomerId == customerId.Value);

        if (accountId.HasValue)
        {
            int targetAccId = accountId.Value;
            if (includeTasweyaId.HasValue)
            {
                int targetTasweyaId = includeTasweyaId.Value;
                query = query.Where(m =>
                    m.AccountShares.Any(s =>
                        s.AccountId == targetAccId &&
                        (!s.IsIncludedInTasweya ||
                         _context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.TasweyaId == targetTasweyaId && link.Tasweya != null && link.Tasweya.AccountId == targetAccId))) ||
                    (!m.AccountShares.Any() && m.Customer != null && m.Customer.AccountId == targetAccId &&
                        (!m.IsIncludedInTasweya || _context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.TasweyaId == targetTasweyaId))));
            }
            else
            {
                query = query.Where(m =>
                    m.AccountShares.Any(s => s.AccountId == targetAccId && !s.IsIncludedInTasweya && !s.IsPaid) ||
                    (!m.AccountShares.Any() && m.Customer != null && m.Customer.AccountId == targetAccId && !m.IsIncludedInTasweya));
            }
        }
        else
        {
            if (includeTasweyaId.HasValue)
            {
                int targetTasweyaId = includeTasweyaId.Value;
                query = query.Where(m =>
                    m.AccountShares.Any(s => !s.IsIncludedInTasweya && !s.IsPaid) ||
                    _context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.TasweyaId == targetTasweyaId) ||
                    (!m.AccountShares.Any() && !m.IsIncludedInTasweya));
            }
            else
            {
                query = query.Where(m =>
                    m.AccountShares.Any(s => !s.IsIncludedInTasweya && !s.IsPaid) ||
                    (!m.AccountShares.Any() && !m.IsIncludedInTasweya));
            }
        }

        var list = await query
            .OrderByDescending(m => m.MoqaysaId)
            .ToListAsync(ct);

        var moqaysaIds = list.Select(m => m.MoqaysaId).ToList();
        var activeLinkMap = await _context.TasweyaMoqaysaLinks
            .AsNoTracking()
            .Where(l => moqaysaIds.Contains(l.MoqaysaId) && l.Tasweya != null && l.Tasweya.IsActive)
            .Select(l => new
            {
                l.MoqaysaId,
                l.Tasweya!.AccountId,
                l.TasweyaId,
                l.Tasweya.SettlementNumber
            })
            .ToListAsync(ct);

        var linkLookup = activeLinkMap
            .GroupBy(x => (x.MoqaysaId, x.AccountId))
            .ToDictionary(g => g.Key, g => g.First());

        return list.Select(m =>
        {
            var shares = m.AccountShares.Select(s =>
            {
                linkLookup.TryGetValue((m.MoqaysaId, s.AccountId), out var activeLink);
                return new MoqaysaAccountShareViewModel
                {
                    ShareId = s.ShareId,
                    AccountId = s.AccountId,
                    AccountNameAr = s.Account?.AccountNameAr ?? string.Empty,
                    DebitHeaderTextAr = s.Account?.DebitHeaderTextAr ?? string.Empty,
                    SharePercentage = s.SharePercentage,
                    AllocatedAmount = s.AllocatedAmount,
                    IsIncludedInTasweya = s.IsIncludedInTasweya,
                    IsPaid = s.IsPaid,
                    PaidAtUtc = s.PaidAtUtc,
                    TasweyaId = activeLink?.TasweyaId,
                    SettlementNumber = activeLink?.SettlementNumber
                };
            }).ToList();

            bool isPartiallyIncluded = shares.Any(s => s.IsIncludedInTasweya) && !m.IsIncludedInTasweya;

            return new TasweyaUnsettledMoqaysaViewModel
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
                SelectedMinistryOfFinancePercent = m.SelectedMinistryOfFinancePercent,
                SelectedWorkersFundPercent = m.SelectedWorkersFundPercent,
                SelectedOtherPercent = m.SelectedOtherPercent,
                SelectedVatPercent = m.SelectedVatPercent,
                MinistryOfFinanceFee = m.MinistryOfFinanceFee,
                WorkersSyndicateFund = m.WorkersSyndicateFund,
                ApplyOther = m.ApplyOther,
                OtherAmount = m.OtherAmount,
                ApplyVat = m.ApplyVat,
                VatAmount = m.VatAmount,
                FinalGrandTotal = m.FinalGrandTotal,
                IsIncludedInTasweya = m.IsIncludedInTasweya,
                IsPartiallyIncludedInTasweya = isPartiallyIncluded,
                CreatedAtUtc = m.CreatedAtUtc,
                AccountShares = shares
            };
        }).ToList();
    }

    public async Task<List<TasweyaSummaryViewModel>> GetAllAsync(
        int? accountId,
        int? customerId,
        bool? isPaid,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.Tasweyat
            .IgnoreQueryFilters()
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
                    .ThenInclude(m => m!.AccountShares)
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
            MoqaysaLinks = t.MoqaysaLinks.Select(ml =>
            {
                var share = ml.Moqaysa?.AccountShares.FirstOrDefault(s => s.AccountId == t.AccountId);
                return new TasweyaMoqaysaItemViewModel
                {
                    LinkId = ml.LinkId,
                    MoqaysaId = ml.MoqaysaId,
                    JobOrderNumber = ml.Moqaysa?.JobOrderNumber ?? "",
                    SharePercentage = share?.SharePercentage ?? 100m,
                    AllocatedAmount = share?.AllocatedAmount ?? (ml.Moqaysa?.FinalGrandTotal ?? 0m),
                    FinalGrandTotal = ml.Moqaysa?.FinalGrandTotal ?? 0m,
                    ApplyVat = ml.Moqaysa?.ApplyVat ?? false,
                    VatAmount = ml.Moqaysa?.VatAmount ?? 0m
                };
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message, TasweyaCreateResultViewModel? Result)> CreateAsync(
        CreateTasweyaDto dto,
        string? userId,
        CancellationToken ct = default)
    {
        var singleAsBatch = new CreateMultiAccountTasweyaDto
        {
            SettlementDate = dto.SettlementDate,
            CustomerId = dto.CustomerId,
            MoqaysaIds = dto.MoqaysaIds,
            OverrideMarkupPercent = dto.OverrideMarkupPercent,
            Accounts = new List<AccountSettlementEntryDto>
            {
                new()
                {
                    AccountId = dto.AccountId,
                    SettlementNumber = dto.SettlementNumber,
                    ClaimNumber = dto.ClaimNumber,
                    MoqaysaIds = dto.MoqaysaIds
                }
            }
        };

        var (success, message, batchResult) = await CreateMultiAccountAsync(singleAsBatch, userId, ct);
        if (!success || batchResult == null || batchResult.Items.Count == 0)
            return (false, message, null);

        var item = batchResult.Items[0];
        item.Message = "تم إصدار كشف التسوية (المطالبة المالية) بنجاح";
        return (true, item.Message, item);
    }

    public async Task<(bool Success, string Message, BatchTasweyaCreateResultViewModel? Result)> CreateMultiAccountAsync(
        CreateMultiAccountTasweyaDto dto,
        string? userId,
        CancellationToken ct = default)
    {
        if (dto.MoqaysaIds == null || dto.MoqaysaIds.Count == 0)
            return (false, "يرجى تحديد مقايسة واحدة على الأقل لإدراجها في كشف التسوية", null);

        if (dto.Accounts == null || dto.Accounts.Count == 0)
            return (false, "يرجى تحديد حساب دافع واحد على الأقل لإصدار التسوية", null);

        if (dto.Accounts.GroupBy(a => a.AccountId).Any(g => g.Count() > 1))
            return (false, "لا يمكن تكرار نفس الحساب الدافع أكثر من مرة في نفس الدفعة", null);

        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, ct);
        if (customer == null)
            return (false, "الجهة الطالبة غير موجودة", null);

        var requestedAccountIds = dto.Accounts.Select(a => a.AccountId).Distinct().ToList();
        var accountsById = await _context.Accounts
            .Where(a => requestedAccountIds.Contains(a.AccountId))
            .ToDictionaryAsync(a => a.AccountId, ct);

        foreach (var accId in requestedAccountIds)
        {
            if (!accountsById.ContainsKey(accId))
                return (false, "أحد الحسابات المختارة غير صحيح أو غير موجود", null);
        }

        var distinctMoqaysaIds = dto.MoqaysaIds.Distinct().ToList();
        var moqaysat = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.AccountShares)
            .Where(m => distinctMoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync(ct);

        if (moqaysat.Count != distinctMoqaysaIds.Count)
            return (false, "بعض المقايسات المحددة غير موجودة أو غير فعالة", null);

        var moqaysatById = moqaysat.ToDictionary(m => m.MoqaysaId);
        var fy = FinancialYearHelper.GetFinancialYear(dto.SettlementDate);

        // Validate each account entry and prepare computed payloads before writing
        var seenSettlementNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var preparedEntries = new List<(
            AccountSettlementEntryDto Entry,
            AccountEntity Account,
            string FinalSettlementNumber,
            List<(Moqaysa Moqaysa, decimal SharePercentage)> Items,
            TasweyaCalculatedTotals Totals,
            string Tafqeet,
            string JobOrdersText)>();

        foreach (var entry in dto.Accounts)
        {
            if (string.IsNullOrWhiteSpace(entry.SettlementNumber))
                return (false, "يرجى كتابة رقم كشف التسوية لكل حساب محدد", null);

            var account = accountsById[entry.AccountId];
            var finalSettlementNumber = FinancialYearHelper.FormatSettlementNumber(entry.SettlementNumber, dto.SettlementDate);

            if (!seenSettlementNumbers.Add(finalSettlementNumber))
                return (false, $"لا يمكن استخدام نفس رقم كشف التسوية '{finalSettlementNumber}' لأكثر من حساب في نفس الوقت.", null);

            var existsInFy = await _context.Tasweyat
                .IgnoreQueryFilters()
                .AnyAsync(t => t.SettlementNumber == finalSettlementNumber &&
                               t.SettlementDate >= fy.StartDate &&
                               t.SettlementDate <= fy.EndDate, ct);
            if (existsInFy)
                return (false, $"رقم كشف التسوية '{finalSettlementNumber}' مسجل بالفعل ضمن السنة المالية ({fy.Code}). يرجى استخدام رقم آخر.", null);

            List<Moqaysa> entryMoqaysat;
            if (entry.MoqaysaIds != null && entry.MoqaysaIds.Count > 0)
            {
                entryMoqaysat = entry.MoqaysaIds
                    .Distinct()
                    .Where(id => moqaysatById.ContainsKey(id))
                    .Select(id => moqaysatById[id])
                    .ToList();
            }
            else
            {
                // Auto-select moqaysat that allocate a positive share to this account
                entryMoqaysat = moqaysat
                    .Where(m => ResolveSharePercentage(m, entry.AccountId) > 0m)
                    .ToList();
            }

            if (entryMoqaysat.Count == 0)
                return (false, $"الحساب ({account.AccountNameAr}) لا يمتلك أي حصة مالية في المقايسات المحددة.", null);

            var entryItems = new List<(Moqaysa Moqaysa, decimal SharePercentage)>();
            foreach (var m in entryMoqaysat)
            {
                decimal pct = ResolveSharePercentage(m, entry.AccountId);
                if (pct <= 0m)
                    return (false, $"المقايسة (إذن تشغيل: {m.JobOrderNumber}) لا تتضمن حصة مالية لحساب ({account.AccountNameAr}).", null);

                entryItems.Add((m, pct));
            }

            var targetMoqaysaIds = entryMoqaysat.Select(m => m.MoqaysaId).ToList();
            var activeConflicts = await _context.TasweyaMoqaysaLinks
                .Include(link => link.Moqaysa)
                .Include(link => link.Tasweya)
                .Where(link => targetMoqaysaIds.Contains(link.MoqaysaId)
                            && link.Tasweya != null
                            && link.Tasweya.IsActive
                            && link.Tasweya.AccountId == entry.AccountId)
                .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
                .Where(jobNo => !string.IsNullOrEmpty(jobNo))
                .Distinct()
                .ToListAsync(ct);

            if (activeConflicts.Count > 0)
                return (false, $"المقايسات ذات أذون التشغيل ({string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط لحساب ({account.AccountNameAr}).", null);

            var totals = CalculateTotals(entryItems, dto.OverrideMarkupPercent);
            string tafqeet = _tafqeetService.ToWords(totals.SettlementGrandTotal);
            string jobOrders = string.Join(" , ", entryMoqaysat.Select(m => m.JobOrderNumber).Distinct());

            preparedEntries.Add((entry, account, finalSettlementNumber, entryItems, totals, tafqeet, jobOrders));
        }

        var createdTasweyat = new List<(Tasweya Entity, string AccountNameAr)>();

        Action buildAll = () =>
        {
            createdTasweyat.Clear();
            foreach (var p in preparedEntries)
            {
                var tasweya = new Tasweya
                {
                    SettlementNumber = p.FinalSettlementNumber,
                    SettlementDate = dto.SettlementDate,
                    AccountId = p.Entry.AccountId,
                    CustomerId = dto.CustomerId,
                    ClaimNumber = string.IsNullOrWhiteSpace(p.Entry.ClaimNumber) ? p.FinalSettlementNumber : p.Entry.ClaimNumber.Trim(),
                    JobOrderNumbersText = p.JobOrdersText,

                    TotalMaterialsSum = p.Totals.TotalMaterialsSum,
                    TotalWorkerWagesSum = p.Totals.TotalWorkerWagesSum,
                    TotalMachineCostsSum = p.Totals.TotalMachineCostsSum,
                    AdminExpensesSum = p.Totals.AdminExpensesSum,
                    MarkupAmountSum = p.Totals.MarkupAmountSum,
                    MinistryOfFinanceSum = p.Totals.MinistryOfFinanceSum,
                    WorkersFundSum = p.Totals.WorkersFundSum,
                    OtherAmountSum = p.Totals.OtherAmountSum,

                    RevenuesTotal = p.Totals.RevenuesTotal,
                    VatTotal = p.Totals.VatTotal,
                    SettlementGrandTotal = p.Totals.SettlementGrandTotal,
                    TotalAmountInWordsAr = p.Tafqeet,

                    IsPaid = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    IsActive = true
                };

                foreach (var (m, _) in p.Items)
                {
                    var share = m.AccountShares.FirstOrDefault(s => s.AccountId == p.Entry.AccountId);
                    if (share != null)
                    {
                        share.IsIncludedInTasweya = true;
                    }
                    SyncMoqaysaTasweyaFlag(m, fallbackWhenNoShares: true);

                    tasweya.MoqaysaLinks.Add(new TasweyaMoqaysaLink
                    {
                        MoqaysaId = m.MoqaysaId
                    });
                }

                _context.Tasweyat.Add(tasweya);
                createdTasweyat.Add((tasweya, p.Account.AccountNameAr));
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
                    buildAll();
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                buildAll();
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save Tasweya due to database conflict.");
            return (false, "تعذر حفظ كشف التسوية: قد يكون رقم كشف التسوية مسجلاً مسبقاً أو هناك تعارض في البيانات", null);
        }

        if (createdTasweyat.Count == 0)
            return (false, "فشل إنشاء كشوف التسوية", null);

        var itemsResult = createdTasweyat.Select(x => new TasweyaCreateResultViewModel
        {
            Message = "تم إصدار كشف التسوية بنجاح",
            TasweyaId = x.Entity.TasweyaId,
            SettlementNumber = x.Entity.SettlementNumber,
            AccountId = x.Entity.AccountId,
            AccountNameAr = x.AccountNameAr,
            VatTotal = x.Entity.VatTotal,
            SettlementGrandTotal = x.Entity.SettlementGrandTotal,
            RevenuesTotal = x.Entity.RevenuesTotal,
            TotalMaterialsSum = x.Entity.TotalMaterialsSum,
            TotalWorkerWagesSum = x.Entity.TotalWorkerWagesSum,
            TotalMachineCostsSum = x.Entity.TotalMachineCostsSum,
            TotalAmountInWordsAr = x.Entity.TotalAmountInWordsAr
        }).ToList();

        var batchResult = new BatchTasweyaCreateResultViewModel
        {
            Message = itemsResult.Count == 1
                ? "تم إصدار كشف التسوية (المطالبة المالية) بنجاح"
                : $"تم إصدار عدد {itemsResult.Count} كشوف تسوية مستقلة بنجاح",
            CreatedCount = itemsResult.Count,
            Items = itemsResult
        };

        return (true, batchResult.Message, batchResult);
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

        var distinctMoqaysaIds = dto.MoqaysaIds.Distinct().ToList();
        var moqaysat = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.AccountShares)
            .Where(m => distinctMoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync(ct);

        if (moqaysat.Count != distinctMoqaysaIds.Count)
            return (false, "بعض المقايسات المحددة غير موجودة أو غير فعالة", null);

        var itemsWithShare = new List<(Moqaysa Moqaysa, decimal SharePercentage)>();
        foreach (var m in moqaysat)
        {
            decimal pct = ResolveSharePercentage(m, dto.AccountId);
            if (pct <= 0m)
                return (false, $"المقايسة (إذن تشغيل: {m.JobOrderNumber}) لا تتضمن حصة مالية لحساب ({account.AccountNameAr}).", null);
            itemsWithShare.Add((m, pct));
        }

        var activeConflicts = await _context.TasweyaMoqaysaLinks
            .Include(link => link.Moqaysa)
            .Include(link => link.Tasweya)
            .Where(link => distinctMoqaysaIds.Contains(link.MoqaysaId)
                        && link.TasweyaId != id
                        && link.Tasweya != null
                        && link.Tasweya.IsActive
                        && link.Tasweya.AccountId == dto.AccountId)
            .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
            .Where(jobNo => !string.IsNullOrEmpty(jobNo))
            .Distinct()
            .ToListAsync(ct);

        if (activeConflicts.Count > 0)
            return (false, $"إحدى المقايسات المحددة (إذن تشغيل: {string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط لحساب ({account.AccountNameAr})", null);

        var totals = CalculateTotals(itemsWithShare, dto.OverrideMarkupPercent);
        string tafqeet = _tafqeetService.ToWords(totals.SettlementGrandTotal);
        string jobOrders = string.Join(" , ", moqaysat.Select(m => m.JobOrderNumber).Distinct());

        Action applyUpdate = () =>
        {
            int previousAccountId = tasweya.AccountId;
            // Release old moqaysat shares belonging to the previous account
            var oldMoqaysaIds = tasweya.MoqaysaLinks.Select(ml => ml.MoqaysaId).Distinct().ToList();
            var oldMoqaysat = _context.Moqaysat
                .Include(m => m.AccountShares)
                .Where(m => oldMoqaysaIds.Contains(m.MoqaysaId))
                .ToList();
            foreach (var om in oldMoqaysat)
            {
                var oldShare = om.AccountShares.FirstOrDefault(s => s.AccountId == previousAccountId);
                if (oldShare != null)
                {
                    oldShare.IsIncludedInTasweya = false;
                }
                SyncMoqaysaTasweyaFlag(om, fallbackWhenNoShares: false);
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
                var newShare = m.AccountShares.FirstOrDefault(s => s.AccountId == dto.AccountId);
                if (newShare != null)
                {
                    newShare.IsIncludedInTasweya = true;
                }
                SyncMoqaysaTasweyaFlag(m, fallbackWhenNoShares: true);
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
            .IgnoreQueryFilters()
            .Include(t => t.MoqaysaLinks)
            .FirstOrDefaultAsync(t => t.TasweyaId == id, ct);

        if (item == null)
            return (false, "كشف التسوية غير موجود", null);

        var moqaysaIds = item.MoqaysaLinks.Select(ml => ml.MoqaysaId).ToList();
        var linkedMoqaysat = await _context.Moqaysat
            .IgnoreQueryFilters()
            .Include(m => m.AccountShares)
            .Where(m => moqaysaIds.Contains(m.MoqaysaId))
            .ToListAsync(ct);

        if (item.IsActive)
        {
            if (item.IsPaid)
                return (false, "لا يمكن إلغاء تفعيل كشف تسوية تم سداده وتأكيده بأمر دفع رسمي", null);

            // CRIT-06: Check active payment order
            bool hasActivePaymentOrder = await _context.PaymentOrderTasweyaLinks
                .AnyAsync(l => l.TasweyaId == id && l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive, ct);
            if (hasActivePaymentOrder)
                return (false, "لا يمكن إلغاء تفعيل كشف تسوية مرتبط بأمر دفع نشط", null);

            Action applyDeactivate = () =>
            {
                foreach (var m in linkedMoqaysat)
                {
                    var share = m.AccountShares.FirstOrDefault(s => s.AccountId == item.AccountId);
                    if (share != null)
                    {
                        share.IsIncludedInTasweya = false;
                    }
                    SyncMoqaysaTasweyaFlag(m, fallbackWhenNoShares: false);
                }

                item.IsActive = false;
            };

            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    applyDeactivate();
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                applyDeactivate();
                await _context.SaveChangesAsync(ct);
            }

            return (true, "تم إلغاء تفعيل كشف التسوية وتحرير حصص المقايسات التابعة لهذا الحساب بنجاح", false);
        }
        else
        {
            var alreadyIncludedInActive = await _context.TasweyaMoqaysaLinks
                .IgnoreQueryFilters()
                .Include(link => link.Moqaysa)
                .Include(link => link.Tasweya)
                .Where(link => moqaysaIds.Contains(link.MoqaysaId)
                            && link.TasweyaId != item.TasweyaId
                            && link.Tasweya != null
                            && link.Tasweya.IsActive
                            && link.Tasweya.AccountId == item.AccountId)
                .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
                .Where(jobNo => !string.IsNullOrEmpty(jobNo))
                .Distinct()
                .ToListAsync(ct);

            if (alreadyIncludedInActive.Count > 0)
            {
                return (false, $"لا يمكن إعادة تفعيل كشف التسوية؛ نظراً لأن المقايسات ذات أذون التشغيل ({string.Join(" ، ", alreadyIncludedInActive)}) تم إدراجها بالفعل لهذا الحساب في كشف تسوية آخر نشط.", null);
            }

            Action applyReactivate = () =>
            {
                foreach (var m in linkedMoqaysat)
                {
                    var share = m.AccountShares.FirstOrDefault(s => s.AccountId == item.AccountId);
                    if (share != null)
                    {
                        share.IsIncludedInTasweya = true;
                    }
                    SyncMoqaysaTasweyaFlag(m, fallbackWhenNoShares: true);
                }

                item.IsActive = true;
            };

            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    applyReactivate();
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            else
            {
                applyReactivate();
                await _context.SaveChangesAsync(ct);
            }

            return (true, "تم إعادة تفعيل كشف التسوية وربط حصص المقايسات التابعة له بنجاح", true);
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

    private static decimal ResolveSharePercentage(Moqaysa m, int accountId)
    {
        var share = m.AccountShares.FirstOrDefault(s => s.AccountId == accountId);
        if (share != null)
            return share.SharePercentage;

        if (m.AccountShares.Count == 0 && m.Customer?.AccountId == accountId)
            return 100m;

        return 0m;
    }

    private static void SyncMoqaysaTasweyaFlag(Moqaysa m, bool fallbackWhenNoShares)
    {
        m.IsIncludedInTasweya = m.AccountShares.Count > 0
            ? m.AccountShares.All(s => s.IsIncludedInTasweya)
            : fallbackWhenNoShares;
    }

    private static TasweyaCalculatedTotals CalculateTotals(
        List<(Moqaysa Moqaysa, decimal SharePercentage)> items,
        decimal? overrideMarkupPercent)
    {
        decimal matSum = 0m, wagesSum = 0m, machSum = 0m, adminSum = 0m;
        decimal markupSum = 0m, mofSum = 0m, workersSum = 0m, otherSum = 0m;
        decimal vatSum = 0m;

        bool hasOverride = overrideMarkupPercent.HasValue && overrideMarkupPercent.Value >= 0m;
        decimal overridePercent = overrideMarkupPercent ?? 0m;

        foreach (var (m, sharePercentage) in items)
        {
            decimal ratio = sharePercentage / 100m;
            decimal mat = MoqaysaCalculationService.Round4(m.TotalMaterials * ratio);
            decimal wages = MoqaysaCalculationService.Round4(m.TotalWorkerWages * ratio);
            decimal mach = MoqaysaCalculationService.Round4(m.TotalMachineCosts * ratio);
            decimal admin = MoqaysaCalculationService.Round4(m.AdminExpenses * ratio);

            if (hasOverride)
            {
                decimal baseExp = mat + wages + mach + admin;
                decimal markup = MoqaysaCalculationService.Round4(baseExp * (overridePercent / 100m));
                decimal afterMarkup = MoqaysaCalculationService.Round4(baseExp + markup);

                decimal diffTaxes = Math.Max(0m, afterMarkup - mat);
                decimal mofPct = m.SelectedMinistryOfFinancePercent > 0m ? m.SelectedMinistryOfFinancePercent / 100m : 0.15m;
                decimal workersPct = m.SelectedWorkersFundPercent > 0m ? m.SelectedWorkersFundPercent / 100m : 0.10m;
                decimal mof = MoqaysaCalculationService.Round4(diffTaxes * mofPct);
                decimal workers = MoqaysaCalculationService.Round4(diffTaxes * workersPct);

                decimal subTotal2 = MoqaysaCalculationService.Round4(afterMarkup + mof + workers);
                decimal otherPct = m.SelectedOtherPercent > 0m ? m.SelectedOtherPercent / 100m : 0.10m;
                decimal other = m.ApplyOther
                    ? MoqaysaCalculationService.Round4(subTotal2 * otherPct)
                    : 0m;

                decimal subTotal3 = MoqaysaCalculationService.Round4(subTotal2 + other);
                decimal vatPct = m.SelectedVatPercent > 0m ? m.SelectedVatPercent / 100m : 0.14m;
                // CRIT-05: VAT is strictly per-Moqaysa ApplyVat!
                decimal vat = m.ApplyVat
                    ? MoqaysaCalculationService.Round4(subTotal3 * vatPct)
                    : 0m;

                matSum += mat;
                wagesSum += wages;
                machSum += mach;
                adminSum += admin;
                markupSum += markup;
                mofSum += mof;
                workersSum += workers;
                otherSum += other;
                vatSum += vat;
            }
            else
            {
                matSum += mat;
                wagesSum += wages;
                machSum += mach;
                adminSum += admin;
                markupSum += MoqaysaCalculationService.Round4(m.MarkupAmount * ratio);
                mofSum += MoqaysaCalculationService.Round4(m.MinistryOfFinanceFee * ratio);
                workersSum += MoqaysaCalculationService.Round4(m.WorkersSyndicateFund * ratio);
                otherSum += MoqaysaCalculationService.Round4(m.OtherAmount * ratio);
                // CRIT-05: Strictly per-Moqaysa ApplyVat check instead of global blanket check!
                vatSum += m.ApplyVat ? MoqaysaCalculationService.Round4(m.VatAmount * ratio) : 0m;
            }
        }

        decimal totalMat = MoqaysaCalculationService.Round2(matSum);
        decimal totalWages = MoqaysaCalculationService.Round2(wagesSum);
        decimal totalMach = MoqaysaCalculationService.Round2(machSum);
        decimal totalAdmin = MoqaysaCalculationService.Round2(adminSum);
        decimal totalMarkup = MoqaysaCalculationService.Round2(markupSum);
        decimal totalMof = MoqaysaCalculationService.Round2(mofSum);
        decimal totalWorkers = MoqaysaCalculationService.Round2(workersSum);
        decimal totalOther = MoqaysaCalculationService.Round2(otherSum);

        // Enforce strict internal double-entry equality (Credit sum == Debit)
        decimal revenuesTotal = MoqaysaCalculationService.Round2(
            totalMat + totalWages + totalMach + totalAdmin + totalMarkup + totalMof + totalWorkers + totalOther);
        decimal vatTotal = MoqaysaCalculationService.Round2(vatSum);
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
