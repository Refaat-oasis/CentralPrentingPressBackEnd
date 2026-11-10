using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.Models;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class PaymentOrderService : IPaymentOrderService
{
    private static readonly Regex OrderNumberRegex = new(@"^[A-Za-z0-9_\-\.]{3,50}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;
    private readonly ITafqeetService _tafqeetService;
    private readonly IAuditLogService _auditLogService;

    public PaymentOrderService(
        ApplicationDbContext context,
        WordExportService wordExportService,
        PdfExportService pdfExportService,
        ITafqeetService tafqeetService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _wordExportService = wordExportService;
        _pdfExportService = pdfExportService;
        _tafqeetService = tafqeetService;
        _auditLogService = auditLogService;
    }

    public async Task<List<UnpaidTasweyaViewModel>> GetUnpaidTasweyatAsync(
        int? accountId,
        int? customerId,
        CancellationToken ct = default)
    {
        var activePaymentOrderTasweyaIds = await _context.PaymentOrderTasweyaLinks
            .AsNoTracking()
            .Where(l => l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive)
            .Select(l => l.TasweyaId)
            .ToListAsync(ct);

        var query = _context.Tasweyat
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .Where(t => t.IsActive && !t.IsPaid && !activePaymentOrderTasweyaIds.Contains(t.TasweyaId));

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        return await query
            .OrderByDescending(t => t.TasweyaId)
            .Select(t => new UnpaidTasweyaViewModel
            {
                TasweyaId = t.TasweyaId,
                SettlementNumber = t.SettlementNumber,
                SettlementDate = t.SettlementDate,
                ClaimNumber = t.ClaimNumber,
                AccountId = t.AccountId,
                AccountNameAr = t.Account != null ? t.Account.AccountNameAr : "غير محدد",
                CustomerId = t.CustomerId,
                CustomerNameAr = t.Customer != null ? t.Customer.CustomerNameAr : "غير محدد",
                JobOrderNumbersText = t.JobOrderNumbersText,
                SettlementGrandTotal = t.SettlementGrandTotal,
                TotalAmountInWordsAr = t.TotalAmountInWordsAr,
                IsPaid = t.IsPaid,
                IsActive = t.IsActive,
                CreatedAtUtc = t.CreatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<List<PaymentOrderSummaryViewModel>> GetAllAsync(
        int? accountId = null,
        int? customerId = null,
        PaymentOrderStatus? status = null,
        bool? isConfirmed = null,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.PaymentOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .Include(p => p.TasweyaLinks)
            .AsQueryable();

        if (accountId.HasValue) query = query.Where(p => p.AccountId == accountId.Value);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (isConfirmed.HasValue) query = query.Where(p => p.IsConfirmed == isConfirmed.Value);
        if (activeOnly) query = query.Where(p => p.IsActive);

        var list = await query
            .OrderByDescending(p => p.PaymentOrderId)
            .ToListAsync(ct);

        return list.Select(p => new PaymentOrderSummaryViewModel
        {
            PaymentOrderId = p.PaymentOrderId,
            PaymentOrderNumber = p.PaymentOrderNumber,
            PaymentDate = p.PaymentDate,
            AccountId = p.AccountId,
            AccountNameAr = p.Account != null ? p.Account.AccountNameAr : "غير محدد",
            CustomerId = p.CustomerId,
            CustomerNameAr = p.Customer != null ? p.Customer.CustomerNameAr : "مجموعة عملاء",
            TotalPaidAmount = p.TotalPaidAmount,
            TotalAmountInWordsAr = p.TotalAmountInWordsAr,
            Status = p.Status,
            StatusTextAr = GetStatusTextAr(p.Status),
            IsConfirmed = p.IsConfirmed,
            ConfirmedAtUtc = p.ConfirmedAtUtc,
            ConfirmedByUserId = p.ConfirmedByUserId,
            Notes = p.Notes,
            IsActive = p.IsActive,
            LinkedTasweyatCount = p.TasweyaLinks.Count,
            CreatedAtUtc = p.CreatedAtUtc
        }).ToList();
    }

    public async Task<PaymentOrderDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (item == null) return null;

        decimal? budgetCeiling = item.Customer?.BudgetCeiling ?? item.Account?.BudgetCeiling;
        decimal? remainingBudget = null;

        bool hasAccountCeiling = item.Account?.BudgetCeiling.HasValue == true && item.Account.BudgetCeiling.Value >= 0m;
        bool hasCustomerCeiling = item.Customer?.BudgetCeiling.HasValue == true && item.Customer.BudgetCeiling.Value >= 0m;

        if (hasAccountCeiling || hasCustomerCeiling)
        {
            decimal minRemaining = decimal.MaxValue;

            if (hasCustomerCeiling && item.CustomerId.HasValue)
            {
                decimal customerAllocated = await _context.PaymentOrders
                    .Where(p => p.IsActive
                             && p.Status != PaymentOrderStatus.Cancelled
                             && p.Status != PaymentOrderStatus.Rejected
                             && p.PaymentOrderId != id
                             && p.CustomerId == item.CustomerId.Value)
                    .SumAsync(p => p.TotalPaidAmount, ct);
                minRemaining = Math.Min(minRemaining, Math.Max(0m, item.Customer!.BudgetCeiling!.Value - customerAllocated - item.TotalPaidAmount));
            }

            if (hasAccountCeiling)
            {
                decimal accountAllocated = await _context.PaymentOrders
                    .Where(p => p.IsActive
                             && p.Status != PaymentOrderStatus.Cancelled
                             && p.Status != PaymentOrderStatus.Rejected
                             && p.PaymentOrderId != id
                             && p.AccountId == item.AccountId)
                    .SumAsync(p => p.TotalPaidAmount, ct);
                minRemaining = Math.Min(minRemaining, Math.Max(0m, item.Account!.BudgetCeiling!.Value - accountAllocated - item.TotalPaidAmount));
            }
            remainingBudget = minRemaining;
        }

        return new PaymentOrderDetailViewModel
        {
            PaymentOrderId = item.PaymentOrderId,
            PaymentOrderNumber = item.PaymentOrderNumber,
            PaymentDate = item.PaymentDate,
            AccountId = item.AccountId,
            Account = item.Account != null ? new AccountViewModel
            {
                AccountId = item.Account.AccountId,
                AccountCode = item.Account.AccountCode,
                AccountNameAr = item.Account.AccountNameAr,
                DebitHeaderTextAr = item.Account.DebitHeaderTextAr,
                BudgetCeiling = item.Account.BudgetCeiling,
                IsActive = item.Account.IsActive
            } : null,
            CustomerId = item.CustomerId,
            Customer = item.Customer != null ? new CustomerViewModel
            {
                CustomerId = item.Customer.CustomerId,
                CustomerNameAr = item.Customer.CustomerNameAr,
                AccountId = item.Customer.AccountId,
                BudgetCeiling = item.Customer.BudgetCeiling,
                Phone = item.Customer.Phone,
                Email = item.Customer.Email,
                Address = item.Customer.Address,
                Notes = item.Customer.Notes,
                IsActive = item.Customer.IsActive
            } : null,
            TotalPaidAmount = item.TotalPaidAmount,
            TotalAmountInWordsAr = item.TotalAmountInWordsAr,
            Status = item.Status,
            StatusTextAr = GetStatusTextAr(item.Status),
            IsConfirmed = item.IsConfirmed,
            ReviewedByUserId = item.ReviewedByUserId,
            ReviewedAtUtc = item.ReviewedAtUtc,
            ConfirmedByUserId = item.ConfirmedByUserId,
            ConfirmedAtUtc = item.ConfirmedAtUtc,
            PaidByUserId = item.PaidByUserId,
            PaidAtUtc = item.PaidAtUtc,
            RejectedByUserId = item.RejectedByUserId,
            RejectedAtUtc = item.RejectedAtUtc,
            RejectionReason = item.RejectionReason,
            CancelledByUserId = item.CancelledByUserId,
            CancelledAtUtc = item.CancelledAtUtc,
            CancellationReason = item.CancellationReason,
            CreatedByUserId = item.CreatedByUserId,
            Notes = item.Notes,
            IsActive = item.IsActive,
            CreatedAtUtc = item.CreatedAtUtc,
            CustomerBudgetCeiling = budgetCeiling,
            RemainingBudget = remainingBudget,
            TasweyaLinks = item.TasweyaLinks.Select(l => new PaymentOrderTasweyaLinkViewModel
            {
                PaymentLinkId = l.PaymentLinkId,
                PaymentOrderId = l.PaymentOrderId,
                TasweyaId = l.TasweyaId,
                SettlementNumber = l.Tasweya?.SettlementNumber ?? "",
                SettlementDate = l.Tasweya?.SettlementDate ?? DateOnly.MinValue,
                ClaimNumber = l.Tasweya?.ClaimNumber ?? "",
                JobOrderNumbersText = l.Tasweya?.JobOrderNumbersText ?? "",
                SnapshotTasweyaAmount = l.SnapshotTasweyaAmount,
                IsActive = l.IsActive
            }).ToList()
        };
    }

    public async Task<PaymentOrderOperationResultViewModel> CreateAsync(
        CreatePaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.PaymentOrderNumber))
            return Failure("يرجى كتابة رقم أمر الدفع الرسمي");

        string orderNumberClean = dto.PaymentOrderNumber.Trim();
        if (!OrderNumberRegex.IsMatch(orderNumberClean))
            return Failure("رقم أمر الدفع غير صالح (يجب أن يتكون من 3-50 حرفاً إنجليزية وأرقاماً وشرطات فقط)");

        if (dto.TasweyaIds == null || dto.TasweyaIds.Count == 0)
            return Failure("يرجى تحديد كشف تسوية واحد على الأقل لسداده");

        if (await _context.PaymentOrders.IgnoreQueryFilters().AnyAsync(p => p.PaymentOrderNumber == orderNumberClean, ct))
            return Failure("رقم أمر الدفع مسجل مسبقاً");

        var tasweyat = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
                .ThenInclude(ml => ml.Moqaysa)
            .Where(t => dto.TasweyaIds.Contains(t.TasweyaId) && t.IsActive)
            .ToListAsync(ct);

        if (tasweyat.Count != dto.TasweyaIds.Count)
            return Failure("بعض كشوف التسوية المحددة غير موجودة أو غير فعالة");

        var activeConflicts = await _context.PaymentOrderTasweyaLinks
            .Include(l => l.PaymentOrder)
            .Include(l => l.Tasweya)
            .Where(l => dto.TasweyaIds.Contains(l.TasweyaId) && l.IsActive && l.PaymentOrder != null && l.PaymentOrder.IsActive)
            .ToListAsync(ct);

        if (activeConflicts.Any())
        {
            var conflictNumbers = string.Join("، ", activeConflicts
                .Select(c => c.Tasweya != null ? c.Tasweya.SettlementNumber : c.TasweyaId.ToString())
                .Distinct());
            return Failure($"كشوف التسوية التالية مدرجة بالفعل في أمر دفع نشط: ({conflictNumbers})");
        }

        int accountId = tasweyat.First().AccountId;
        int? customerId = tasweyat.Select(t => t.CustomerId).Distinct().Count() == 1 ? tasweyat.First().CustomerId : null;
        decimal totalPaidAmount = MoqaysaCalculationService.Round2(tasweyat.Sum(t => t.SettlementGrandTotal));

        // CRIT-08: Validate Budget Ceiling
        var budgetCheck = await CheckBudgetLimitsAsync(accountId, customerId, totalPaidAmount, null, ct);
        if (!budgetCheck.IsValid)
        {
            return new PaymentOrderOperationResultViewModel
            {
                Success = false,
                Message = budgetCheck.FailureMessage ?? $"مبلغ أمر الدفع ({totalPaidAmount:N2} ج.م) يتجاوز السقف المالي المعتمد للموازنة.",
                TotalPaidAmount = totalPaidAmount,
                RemainingBudget = budgetCheck.RemainingBudget
            };
        }

        string tafqeet = _tafqeetService.ToWords(totalPaidAmount);
        var initialStatus = dto.ConfirmImmediately ? PaymentOrderStatus.Approved : PaymentOrderStatus.Draft;
        var now = DateTime.UtcNow;

        var paymentOrder = new PaymentOrder
        {
            PaymentOrderNumber = orderNumberClean,
            PaymentDate = dto.PaymentDate,
            AccountId = accountId,
            CustomerId = customerId,
            TotalPaidAmount = totalPaidAmount,
            TotalAmountInWordsAr = tafqeet,
            Status = initialStatus,
            IsConfirmed = dto.ConfirmImmediately,
            ConfirmedAtUtc = dto.ConfirmImmediately ? now : null,
            ConfirmedByUserId = dto.ConfirmImmediately ? currentUserId : null,
            CreatedByUserId = currentUserId,
            Notes = dto.Notes?.Trim(),
            CreatedAtUtc = now,
            IsActive = true
        };

        foreach (var t in tasweyat)
        {
            paymentOrder.TasweyaLinks.Add(new PaymentOrderTasweyaLink
            {
                TasweyaId = t.TasweyaId,
                SnapshotTasweyaAmount = t.SettlementGrandTotal,
                IsActive = true
            });
        }

        if (dto.ConfirmImmediately)
        {
            foreach (var t in tasweyat)
            {
                t.IsPaid = true;
                t.PaidAtUtc = now;

                foreach (var ml in t.MoqaysaLinks)
                {
                    if (ml.Moqaysa != null)
                    {
                        ml.Moqaysa.IsPaid = true;
                        ml.Moqaysa.PaidAtUtc = now;
                    }
                }
            }
        }

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                    _context.PaymentOrders.Add(paymentOrder);
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            catch (DbUpdateException)
            {
                return Failure("تعذر حفظ أمر الدفع بسبب تعارض في التزامن أو وجود كشف تسوية مدرج في أمر دفع نشط آخر.");
            }
        }
        else
        {
            try
            {
                _context.PaymentOrders.Add(paymentOrder);
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Failure("تعذر حفظ أمر الدفع بسبب تعارض في التزامن أو وجود كشف تسوية مدرج في أمر دفع نشط آخر.");
            }
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: paymentOrder.PaymentOrderId.ToString(),
            actionType: dto.ConfirmImmediately ? "PaymentOrderCreatedAndApproved" : "PaymentOrderCreatedDraft",
            oldValues: null,
            newValues: new { paymentOrder.PaymentOrderId, paymentOrder.PaymentOrderNumber, Status = initialStatus.ToString(), paymentOrder.TotalPaidAmount },
            userId: currentUserId,
            ct: ct);

        return new PaymentOrderOperationResultViewModel
        {
            Success = true,
            Message = paymentOrder.IsConfirmed
                ? "تم إصدار وتأكيد أمر الدفع واعتماد السداد رسمياً بنجاح"
                : "تم حفظ أمر الدفع كمسودة (بانتظار التأكيد للاعتماد)",
            PaymentOrderId = paymentOrder.PaymentOrderId,
            Status = paymentOrder.Status,
            TotalPaidAmount = paymentOrder.TotalPaidAmount
        };
    }

    public async Task<PaymentOrderOperationResultViewModel> UpdateAsync(
        int id,
        UpdatePaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .Include(p => p.TasweyaLinks)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (paymentOrder == null)
            return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("لا يمكن تعديل أمر دفع ملغي");

        if (paymentOrder.Status >= PaymentOrderStatus.Approved)
        {
            paymentOrder.PaymentDate = dto.PaymentDate;
            paymentOrder.Notes = dto.Notes?.Trim();
            await _context.SaveChangesAsync(ct);
            return Success("تم تحديث ملاحظات وتاريخ أمر الدفع المعتمد بنجاح", paymentOrder);
        }

        if (string.IsNullOrWhiteSpace(dto.PaymentOrderNumber))
            return Failure("يرجى إدخال رقم أمر الدفع");

        string orderNumberClean = dto.PaymentOrderNumber.Trim();
        if (!OrderNumberRegex.IsMatch(orderNumberClean))
            return Failure("رقم أمر الدفع غير صالح");

        if (dto.TasweyaIds == null || dto.TasweyaIds.Count == 0)
            return Failure("يرجى تحديد كشف تسوية واحد على الأقل");

        if (await _context.PaymentOrders.IgnoreQueryFilters().AnyAsync(p => p.PaymentOrderNumber == orderNumberClean && p.PaymentOrderId != id, ct))
            return Failure("رقم أمر الدفع مسجل مسبقاً");

        var tasweyat = await _context.Tasweyat
            .Where(t => dto.TasweyaIds.Contains(t.TasweyaId) && t.IsActive)
            .ToListAsync(ct);

        if (tasweyat.Count != dto.TasweyaIds.Count)
            return Failure("بعض كشوف التسوية المحددة غير موجودة أو غير فعالة");

        var activeConflicts = await _context.PaymentOrderTasweyaLinks
            .Include(l => l.PaymentOrder)
            .Include(l => l.Tasweya)
            .Where(l => dto.TasweyaIds.Contains(l.TasweyaId)
                     && l.PaymentOrderId != id
                     && l.IsActive
                     && l.PaymentOrder != null
                     && l.PaymentOrder.IsActive)
            .ToListAsync(ct);

        if (activeConflicts.Any())
        {
            var conflictNumbers = string.Join("، ", activeConflicts
                .Select(c => c.Tasweya != null ? c.Tasweya.SettlementNumber : c.TasweyaId.ToString())
                .Distinct());
            return Failure($"كشوف التسوية التالية مدرجة بالفعل في أمر دفع نشط آخر: ({conflictNumbers})");
        }

        int accountId = tasweyat.First().AccountId;
        int? customerId = tasweyat.Select(t => t.CustomerId).Distinct().Count() == 1 ? tasweyat.First().CustomerId : null;
        decimal totalPaidAmount = MoqaysaCalculationService.Round2(tasweyat.Sum(t => t.SettlementGrandTotal));

        // CRIT-08: Validate Budget Ceiling
        var budgetCheck = await CheckBudgetLimitsAsync(accountId, customerId, totalPaidAmount, id, ct);
        if (!budgetCheck.IsValid)
        {
            return new PaymentOrderOperationResultViewModel
            {
                Success = false,
                Message = budgetCheck.FailureMessage ?? $"مبلغ أمر الدفع ({totalPaidAmount:N2} ج.م) يتجاوز السقف المالي المعتمد للموازنة.",
                TotalPaidAmount = totalPaidAmount,
                RemainingBudget = budgetCheck.RemainingBudget
            };
        }

        string tafqeet = _tafqeetService.ToWords(totalPaidAmount);

        paymentOrder.PaymentOrderNumber = orderNumberClean;
        paymentOrder.PaymentDate = dto.PaymentDate;
        paymentOrder.AccountId = accountId;
        paymentOrder.CustomerId = customerId;
        paymentOrder.TotalPaidAmount = totalPaidAmount;
        paymentOrder.TotalAmountInWordsAr = tafqeet;
        paymentOrder.Notes = dto.Notes?.Trim();

        _context.PaymentOrderTasweyaLinks.RemoveRange(paymentOrder.TasweyaLinks);
        paymentOrder.TasweyaLinks.Clear();

        foreach (var t in tasweyat)
        {
            paymentOrder.TasweyaLinks.Add(new PaymentOrderTasweyaLink
            {
                PaymentOrderId = paymentOrder.PaymentOrderId,
                TasweyaId = t.TasweyaId,
                SnapshotTasweyaAmount = t.SettlementGrandTotal,
                IsActive = true
            });
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("تم تعديل أمر الدفع بواسطة مستخدم آخر في نفس اللحظة. يرجى إعادة المحاولة.");
        }

        return Success("تم حفظ تعديلات أمر الدفع بنجاح", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> SubmitForReviewAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);
        if (paymentOrder == null) return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("لا يمكن إرسال أمر دفع ملغي للمراجعة");

        if (paymentOrder.Status != PaymentOrderStatus.Draft)
            return Failure($"لا يمكن إرسال أمر الدفع للمراجعة إلا إذا كان بحالة مسودة. الحالة الحالية: {GetStatusTextAr(paymentOrder.Status)}");

        var oldStatus = paymentOrder.Status;
        paymentOrder.Status = PaymentOrderStatus.UnderReview;
        paymentOrder.ReviewedByUserId = currentUserId;
        paymentOrder.ReviewedAtUtc = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("تم تعديل حالة أمر الدفع بواسطة مستخدم آخر. يرجى تحديث الصفحة.");
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: id.ToString(),
            actionType: "PaymentOrderSubmittedForReview",
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = paymentOrder.Status.ToString(), paymentOrder.ReviewedByUserId, paymentOrder.ReviewedAtUtc },
            userId: currentUserId,
            ct: ct);

        return Success("تم إرسال أمر الدفع للمراجعة والتدقيق المالي بنجاح", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> ApproveAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.MoqaysaLinks)
                        .ThenInclude(ml => ml.Moqaysa)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (paymentOrder == null) return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("لا يمكن اعتماد أمر دفع ملغي");

        if (paymentOrder.Status == PaymentOrderStatus.Approved || paymentOrder.Status == PaymentOrderStatus.Paid)
            return Failure("أمر الدفع معتمد بالفعل مسبقاً");

        if (paymentOrder.Status != PaymentOrderStatus.Draft && paymentOrder.Status != PaymentOrderStatus.UnderReview)
            return Failure($"لا يمكن اعتماد أمر الدفع من حالته الحالية ({GetStatusTextAr(paymentOrder.Status)})");

        // Validate Budget Ceiling
        var budgetCheck = await CheckBudgetLimitsAsync(paymentOrder.AccountId, paymentOrder.CustomerId, paymentOrder.TotalPaidAmount, id, ct);
        if (!budgetCheck.IsValid)
        {
            return new PaymentOrderOperationResultViewModel
            {
                Success = false,
                Message = budgetCheck.FailureMessage ?? $"مبلغ أمر الدفع ({paymentOrder.TotalPaidAmount:N2} ج.م) يتجاوز السقف المالي المعتمد للموازنة.",
                TotalPaidAmount = paymentOrder.TotalPaidAmount,
                RemainingBudget = budgetCheck.RemainingBudget
            };
        }

        var oldStatus = paymentOrder.Status;
        var now = DateTime.UtcNow;

        paymentOrder.Status = PaymentOrderStatus.Approved;
        paymentOrder.IsConfirmed = true;
        paymentOrder.ConfirmedAtUtc = now;
        paymentOrder.ConfirmedByUserId = currentUserId;

        foreach (var link in paymentOrder.TasweyaLinks)
        {
            if (link.Tasweya != null)
            {
                link.Tasweya.IsPaid = true;
                link.Tasweya.PaidAtUtc = now;

                if (link.Tasweya.MoqaysaLinks != null)
                {
                    foreach (var ml in link.Tasweya.MoqaysaLinks)
                    {
                        if (ml.Moqaysa != null)
                        {
                            ml.Moqaysa.IsPaid = true;
                            ml.Moqaysa.PaidAtUtc = now;
                        }
                    }
                }
            }
        }

        // HIGH-15: Concurrency check with RowVersion
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("تم اعتماد أو تعديل أمر الدفع بواسطة مستخدم آخر في نفس اللحظة. يرجى تحديث الصفحة والمحاولة مجدداً.");
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: id.ToString(),
            actionType: "PaymentOrderApproved",
            oldValues: new { Status = oldStatus.ToString(), IsConfirmed = false },
            newValues: new { Status = paymentOrder.Status.ToString(), IsConfirmed = true, paymentOrder.ConfirmedAtUtc, paymentOrder.ConfirmedByUserId },
            userId: currentUserId,
            ct: ct);

        return Success("تم اعتماد وتأكيد أمر الدفع بنجاح، وتحولت كافة كشوف التسوية والمقايسات المرتبطة به إلى مسددة رسمياً", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> ExecutePaymentAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);
        if (paymentOrder == null) return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("لا يمكن تنفيذ سداد لأمر دفع ملغي");

        if (paymentOrder.Status != PaymentOrderStatus.Approved)
            return Failure($"لا يمكن تنفيذ السداد إلا بعد اعتماد أمر الدفع رسمياً. الحالة الحالية: {GetStatusTextAr(paymentOrder.Status)}");

        var oldStatus = paymentOrder.Status;
        var now = DateTime.UtcNow;

        paymentOrder.Status = PaymentOrderStatus.Paid;
        paymentOrder.PaidAtUtc = now;
        paymentOrder.PaidByUserId = currentUserId;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("تم تعديل أمر الدفع بواسطة مستخدم آخر في نفس اللحظة.");
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: id.ToString(),
            actionType: "PaymentOrderPaid",
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = paymentOrder.Status.ToString(), paymentOrder.PaidAtUtc, paymentOrder.PaidByUserId },
            userId: currentUserId,
            ct: ct);

        return Success("تم تسجيل تنفيذ السداد البنكي لأمر الدفع بنجاح", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> RejectAsync(
        int id,
        UpdatePaymentOrderStatusDto dto,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);
        if (paymentOrder == null) return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("لا يمكن رفض أمر دفع ملغي");

        if (paymentOrder.Status == PaymentOrderStatus.Paid)
            return Failure("لا يمكن رفض أمر دفع تم سداده وصرفه بالفعل");

        var oldStatus = paymentOrder.Status;
        var now = DateTime.UtcNow;

        paymentOrder.Status = PaymentOrderStatus.Rejected;
        paymentOrder.RejectedByUserId = currentUserId;
        paymentOrder.RejectedAtUtc = now;
        paymentOrder.RejectionReason = dto.Reason?.Trim() ?? "تم الرفض بواسطة المراجع المالي";

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("تم تعديل أمر الدفع بواسطة مستخدم آخر في نفس اللحظة.");
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: id.ToString(),
            actionType: "PaymentOrderRejected",
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = paymentOrder.Status.ToString(), paymentOrder.RejectionReason, paymentOrder.RejectedByUserId },
            userId: currentUserId,
            ct: ct);

        return Success("تم رفض أمر الدفع وحفظ أسباب الرفض بنجاح", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> CancelAsync(
        int id,
        CancelPaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var paymentOrder = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.MoqaysaLinks)
                        .ThenInclude(ml => ml.Moqaysa)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (paymentOrder == null) return Failure("أمر الدفع غير موجود");

        if (!paymentOrder.IsActive || paymentOrder.Status == PaymentOrderStatus.Cancelled)
            return Failure("أمر الدفع ملغي بالفعل مسبقاً");

        var oldStatus = paymentOrder.Status;
        var now = DateTime.UtcNow;

        Action applyCancel = () =>
        {
            paymentOrder.Status = PaymentOrderStatus.Cancelled;
            paymentOrder.IsActive = false;
            paymentOrder.IsConfirmed = false;
            paymentOrder.CancelledByUserId = currentUserId;
            paymentOrder.CancelledAtUtc = now;
            paymentOrder.CancellationReason = dto.Reason.Trim();

            foreach (var link in paymentOrder.TasweyaLinks)
            {
                link.IsActive = false;

                if (link.Tasweya != null)
                {
                    link.Tasweya.IsPaid = false;
                    link.Tasweya.PaidAtUtc = null;

                    if (link.Tasweya.MoqaysaLinks != null)
                    {
                        foreach (var ml in link.Tasweya.MoqaysaLinks)
                        {
                            if (ml.Moqaysa != null)
                            {
                                ml.Moqaysa.IsPaid = false;
                                ml.Moqaysa.PaidAtUtc = null;
                            }
                        }
                    }
                }
            }
        };

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                applyCancel();
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });
        }
        else
        {
            applyCancel();
            await _context.SaveChangesAsync(ct);
        }

        await _auditLogService.LogStatusTransitionAsync(
            moduleName: "PAYMENT_ORDERS",
            entityName: "PaymentOrder",
            recordId: id.ToString(),
            actionType: "PaymentOrderCancelled",
            oldValues: new { Status = oldStatus.ToString(), IsActive = true },
            newValues: new { Status = paymentOrder.Status.ToString(), IsActive = false, paymentOrder.CancellationReason },
            userId: currentUserId,
            ct: ct);

        return Success("تم إلغاء أمر الدفع بنجاح مع استعادة رصيد الموازنة وتحرير كشوف التسوية المرتبطة", paymentOrder);
    }

    public async Task<PaymentOrderOperationResultViewModel> ConfirmAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default)
    {
        return await ApproveAsync(id, currentUserId, ct);
    }

    public async Task<(bool Success, string Message, bool? IsActive)> ToggleActiveAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var item = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.MoqaysaLinks)
                        .ThenInclude(ml => ml.Moqaysa)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (item == null)
            return (false, "أمر الدفع غير موجود", null);

        if (item.IsActive)
        {
            var cancelRes = await CancelAsync(id, new CancelPaymentOrderDto { Reason = "إلغاء تفعيل أمر الدفع عبر إدارة النظام" }, currentUserId, ct);
            return (cancelRes.Success, cancelRes.Message, false);
        }
        else
        {
            var tasweyaIds = item.TasweyaLinks.Select(l => l.TasweyaId).ToList();
            var conflictingLinks = await _context.PaymentOrderTasweyaLinks
                .Include(l => l.PaymentOrder)
                .Include(l => l.Tasweya)
                .Where(l => tasweyaIds.Contains(l.TasweyaId)
                         && l.PaymentOrderId != item.PaymentOrderId
                         && l.IsActive
                         && l.PaymentOrder != null
                         && l.PaymentOrder.IsActive)
                .ToListAsync(ct);

            if (conflictingLinks.Any())
            {
                var conflictNumbers = string.Join("، ", conflictingLinks
                    .Select(c => c.Tasweya != null ? c.Tasweya.SettlementNumber : c.TasweyaId.ToString())
                    .Distinct());
                return (false, $"لا يمكن إعادة تفعيل أمر الدفع لأن كشوف التسوية التالية مدرجة بالفعل في أمر دفع نشط آخر: ({conflictNumbers})", null);
            }

            Action applyReactivate = () =>
            {
                item.IsActive = true;
                item.Status = PaymentOrderStatus.Draft;
                item.CancelledAtUtc = null;
                item.CancelledByUserId = null;
                item.CancellationReason = null;

                foreach (var link in item.TasweyaLinks)
                {
                    link.IsActive = true;
                }
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

            return (true, "تمت إعادة تفعيل أمر الدفع بنجاح كمسودة", true);
        }
    }

    public async Task<BudgetValidationResultViewModel> ValidateBudgetAsync(
        int accountId,
        decimal amount,
        int? customerId = null,
        CancellationToken ct = default)
    {
        var account = await _context.Accounts.FindAsync(new object[] { accountId }, ct);
        if (account == null)
        {
            return new BudgetValidationResultViewModel
            {
                IsValid = false,
                RequestedAmount = amount,
                Message = "الحساب غير موجود"
            };
        }

        var customer = customerId.HasValue
            ? await _context.Customers.FindAsync(new object[] { customerId.Value }, ct)
            : null;

        bool hasAccountCeiling = account.BudgetCeiling.HasValue && account.BudgetCeiling.Value >= 0m;
        bool hasCustomerCeiling = customer != null && customer.BudgetCeiling.HasValue && customer.BudgetCeiling.Value >= 0m;

        if (!hasAccountCeiling && !hasCustomerCeiling)
        {
            return new BudgetValidationResultViewModel
            {
                IsValid = true,
                RequestedAmount = amount,
                RemainingBudget = 999999999m,
                Message = "الحساب لا يخضع لسقف اعتماد مالي محدد"
            };
        }

        decimal minRemaining = decimal.MaxValue;
        decimal? effectiveCeiling = null;
        decimal totalAllocated = 0m;

        if (hasCustomerCeiling && customerId.HasValue)
        {
            decimal cCeiling = customer!.BudgetCeiling!.Value;
            decimal cAllocated = await _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.CustomerId == customerId.Value)
                .SumAsync(p => p.TotalPaidAmount, ct);
            decimal cRem = Math.Max(0m, cCeiling - cAllocated);
            minRemaining = Math.Min(minRemaining, cRem);
            effectiveCeiling = cCeiling;
            totalAllocated = cAllocated;
        }

        if (hasAccountCeiling)
        {
            decimal aCeiling = account.BudgetCeiling!.Value;
            decimal aAllocated = await _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.AccountId == accountId)
                .SumAsync(p => p.TotalPaidAmount, ct);
            decimal aRem = Math.Max(0m, aCeiling - aAllocated);
            if (!effectiveCeiling.HasValue || aRem < minRemaining)
            {
                minRemaining = aRem;
                effectiveCeiling = aCeiling;
                totalAllocated = aAllocated;
            }
        }

        bool isValid = amount <= minRemaining;

        return new BudgetValidationResultViewModel
        {
            IsValid = isValid,
            BudgetCeiling = effectiveCeiling,
            TotalAllocated = totalAllocated,
            RequestedAmount = amount,
            RemainingBudget = minRemaining,
            Message = isValid
                ? "المبلغ المطلوب يقع ضمن سقف الاعتماد المالي المتاح"
                : $"المبلغ المطلوب ({amount:F2} ج.م) يتجاوز سقف الاعتماد المالي المتبقي ({minRemaining:F2} ج.م)"
        };
    }

    private async Task<(bool IsValid, decimal? EffectiveCeiling, decimal RemainingBudget, string? FailureMessage)> CheckBudgetLimitsAsync(
        int accountId,
        int? customerId,
        decimal requestedAmount,
        int? excludePaymentOrderId,
        CancellationToken ct)
    {
        var account = await _context.Accounts.FindAsync(new object[] { accountId }, ct);
        if (account == null)
            return (false, null, 0m, "الحساب غير موجود");

        var customer = customerId.HasValue
            ? await _context.Customers.FindAsync(new object[] { customerId.Value }, ct)
            : null;

        bool hasAccountCeiling = account.BudgetCeiling.HasValue && account.BudgetCeiling.Value >= 0m;
        bool hasCustomerCeiling = customer != null && customer.BudgetCeiling.HasValue && customer.BudgetCeiling.Value >= 0m;

        if (!hasAccountCeiling && !hasCustomerCeiling)
        {
            return (true, null, 999999999m, null);
        }

        // 1. Check Customer-Level Ceiling
        if (hasCustomerCeiling && customerId.HasValue)
        {
            decimal customerCeiling = customer!.BudgetCeiling!.Value;
            var query = _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.CustomerId == customerId.Value);

            if (excludePaymentOrderId.HasValue)
                query = query.Where(p => p.PaymentOrderId != excludePaymentOrderId.Value);

            decimal customerAllocated = await query.SumAsync(p => p.TotalPaidAmount, ct);
            decimal customerRemaining = customerCeiling - customerAllocated;

            if (requestedAmount > customerRemaining)
            {
                decimal safeRemaining = Math.Max(0m, customerRemaining);
                return (false, customerCeiling, safeRemaining,
                    $"مبلغ أمر الدفع ({requestedAmount:N2} ج.م) يتجاوز السقف المالي المعتمد للموازنة. الرصيد المتبقي: ({safeRemaining:N2} ج.م)");
            }
        }

        // 2. Check Account-Level Ceiling (sums across ALL customers under account)
        if (hasAccountCeiling)
        {
            decimal accountCeiling = account.BudgetCeiling!.Value;
            var query = _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.AccountId == accountId);

            if (excludePaymentOrderId.HasValue)
                query = query.Where(p => p.PaymentOrderId != excludePaymentOrderId.Value);

            decimal accountAllocated = await query.SumAsync(p => p.TotalPaidAmount, ct);
            decimal accountRemaining = accountCeiling - accountAllocated;

            if (requestedAmount > accountRemaining)
            {
                decimal safeRemaining = Math.Max(0m, accountRemaining);
                return (false, accountCeiling, safeRemaining,
                    $"مبلغ أمر الدفع ({requestedAmount:N2} ج.م) يتجاوز السقف المالي المعتمد للموازنة. الرصيد المتبقي: ({safeRemaining:N2} ج.م)");
            }
        }

        // Compute most restrictive remaining budget for reporting
        decimal? effectiveCeiling = null;
        decimal effectiveRemaining = 999999999m;

        if (hasCustomerCeiling && customerId.HasValue)
        {
            decimal cCeiling = customer!.BudgetCeiling!.Value;
            var cQuery = _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.CustomerId == customerId.Value);
            if (excludePaymentOrderId.HasValue)
                cQuery = cQuery.Where(p => p.PaymentOrderId != excludePaymentOrderId.Value);
            decimal cAllocated = await cQuery.SumAsync(p => p.TotalPaidAmount, ct);
            decimal cRem = Math.Max(0m, cCeiling - cAllocated);
            effectiveCeiling = cCeiling;
            effectiveRemaining = cRem;
        }

        if (hasAccountCeiling)
        {
            decimal aCeiling = account.BudgetCeiling!.Value;
            var aQuery = _context.PaymentOrders
                .Where(p => p.IsActive
                         && p.Status != PaymentOrderStatus.Cancelled
                         && p.Status != PaymentOrderStatus.Rejected
                         && p.AccountId == accountId);
            if (excludePaymentOrderId.HasValue)
                aQuery = aQuery.Where(p => p.PaymentOrderId != excludePaymentOrderId.Value);
            decimal aAllocated = await aQuery.SumAsync(p => p.TotalPaidAmount, ct);
            decimal aRem = Math.Max(0m, aCeiling - aAllocated);

            if (!effectiveCeiling.HasValue || aRem < effectiveRemaining)
            {
                effectiveCeiling = aCeiling;
                effectiveRemaining = aRem;
            }
        }

        return (true, effectiveCeiling, effectiveRemaining, null);
    }

    public async Task<PaymentOrderOperationResultViewModel> UpdateStatusAsync(
        int id,
        UpdatePaymentOrderStatusDto dto,
        string? currentUserId,
        CancellationToken ct = default)
    {
        return dto.Status switch
        {
            PaymentOrderStatus.UnderReview => await SubmitForReviewAsync(id, currentUserId, ct),
            PaymentOrderStatus.Approved => await ApproveAsync(id, currentUserId, ct),
            PaymentOrderStatus.Paid => await ExecutePaymentAsync(id, currentUserId, ct),
            PaymentOrderStatus.Rejected => await RejectAsync(id, dto, currentUserId, ct),
            PaymentOrderStatus.Cancelled => await CancelAsync(id, new CancelPaymentOrderDto { Reason = dto.Notes ?? "إلغاء أمر الدفع" }, currentUserId, ct),
            _ => Failure("انتقال غير مسموح به في دورة حياة أمر الدفع")
        };
    }

    public async Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Account)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (item == null) return null;

        return _wordExportService.ExportPaymentOrderToWord(item);
    }

    public async Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.PaymentOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Account)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id, ct);

        if (item == null) return null;

        return _pdfExportService.ExportPaymentOrderToPdf(item);
    }

    private static string GetStatusTextAr(PaymentOrderStatus status) => status switch
    {
        PaymentOrderStatus.Draft => "مسودة",
        PaymentOrderStatus.UnderReview => "قيد المراجعة",
        PaymentOrderStatus.Approved => "معتمد",
        PaymentOrderStatus.Paid => "تم السداد",
        PaymentOrderStatus.Rejected => "مرفوض",
        PaymentOrderStatus.Cancelled => "ملغي",
        _ => "غير محدد"
    };

    private static PaymentOrderOperationResultViewModel Failure(string message) => new()
    {
        Success = false,
        Message = message
    };

    private static PaymentOrderOperationResultViewModel Success(string message, PaymentOrder paymentOrder) => new()
    {
        Success = true,
        Message = message,
        PaymentOrderId = paymentOrder.PaymentOrderId,
        Status = paymentOrder.Status,
        TotalPaidAmount = paymentOrder.TotalPaidAmount
    };
}
