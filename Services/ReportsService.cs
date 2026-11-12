using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _context;

    public ReportsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountSummaryViewModel>> GetAccountsSummaryAsync(CancellationToken ct = default)
    {
        var moqaysatAgg = await _context.MoqaysaAccountShares
            .Where(s => s.Moqaysa != null && s.Moqaysa.IsActive)
            .GroupBy(s => s.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalBilled = g.Sum(s => s.AllocatedAmount),
                UnsettledMoqaysat = g.Sum(s => !s.IsIncludedInTasweya ? s.AllocatedAmount : 0m),
                MoqaysatCount = g.Select(s => s.MoqaysaId).Distinct().Count()
            })
            .ToListAsync(ct);

        var tasweyatAgg = await _context.Tasweyat
            .Where(t => t.IsActive)
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalClaimed = g.Sum(t => t.SettlementGrandTotal),
                TotalPaid = g.Sum(t => t.IsPaid ? t.SettlementGrandTotal : 0m),
                UnpaidClaims = g.Sum(t => !t.IsPaid ? t.SettlementGrandTotal : 0m),
                TasweyatCount = g.Count(),
                PaidTasweyatCount = g.Sum(t => t.IsPaid ? 1 : 0)
            })
            .ToListAsync(ct);

        var accounts = await _context.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountId)
            .ToListAsync(ct);

        var result = new List<AccountSummaryViewModel>();

        foreach (var acc in accounts)
        {
            var moq = moqaysatAgg.FirstOrDefault(m => m.AccountId == acc.AccountId);
            var tas = tasweyatAgg.FirstOrDefault(t => t.AccountId == acc.AccountId);

            decimal totalBilled = MoqaysaCalculationService.Round2(moq?.TotalBilled ?? 0m);
            decimal totalClaimed = MoqaysaCalculationService.Round2(tas?.TotalClaimed ?? 0m);
            decimal totalPaid = MoqaysaCalculationService.Round2(tas?.TotalPaid ?? 0m);
            decimal unpaidClaimsInTasweya = MoqaysaCalculationService.Round2(tas?.UnpaidClaims ?? 0m);
            decimal unsettledMoqaysat = MoqaysaCalculationService.Round2(moq?.UnsettledMoqaysat ?? 0m);
            decimal totalUnpaid = Math.Max(0m, MoqaysaCalculationService.Round2(totalBilled - totalPaid));

            result.Add(new AccountSummaryViewModel
            {
                AccountId = acc.AccountId,
                AccountCode = acc.AccountCode,
                AccountNameAr = acc.AccountNameAr,
                DebitHeaderTextAr = acc.DebitHeaderTextAr,
                TotalBilledAmount = totalBilled,
                TotalClaimedInTasweya = totalClaimed,
                TotalPaidAmount = totalPaid,
                TotalUnpaidAmount = totalUnpaid,
                UnpaidClaimsInTasweya = unpaidClaimsInTasweya,
                UnsettledMoqaysat = unsettledMoqaysat,
                MoqaysatCount = moq?.MoqaysatCount ?? 0,
                TasweyatCount = tas?.TasweyatCount ?? 0,
                PaidTasweyatCount = tas?.PaidTasweyatCount ?? 0
            });
        }

        return result;
    }

    public async Task<List<CustomerSummaryViewModel>> GetCustomersSummaryAsync(CustomerSummaryFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.Customers
            .Include(c => c.Account)
            .Include(c => c.Moqaysat.Where(m => m.IsActive))
            .Include(c => c.Tasweyat.Where(t => t.IsActive))
            .Where(c => c.IsActive);

        if (filter.CustomerId.HasValue) query = query.Where(c => c.CustomerId == filter.CustomerId.Value);
        if (filter.AccountId.HasValue) query = query.Where(c => c.AccountId == filter.AccountId.Value);

        var customers = await query.OrderBy(c => c.CustomerNameAr).ToListAsync(ct);

        return customers.Select(c =>
        {
            decimal totalBilled = MoqaysaCalculationService.Round2(c.Moqaysat.Sum(m => m.FinalGrandTotal));
            decimal totalPaid = MoqaysaCalculationService.Round2(c.Tasweyat.Where(t => t.IsPaid).Sum(t => t.SettlementGrandTotal));
            decimal totalUnpaid = Math.Max(0m, MoqaysaCalculationService.Round2(totalBilled - totalPaid));

            return new CustomerSummaryViewModel
            {
                CustomerId = c.CustomerId,
                CustomerNameAr = c.CustomerNameAr,
                AccountId = c.AccountId,
                AccountNameAr = c.Account?.AccountNameAr ?? "غير محدد",
                TotalBilledAmount = totalBilled,
                TotalPaidAmount = totalPaid,
                TotalUnpaidAmount = totalUnpaid,
                MoqaysatCount = c.Moqaysat.Count,
                TasweyatCount = c.Tasweyat.Count,
                PaidTasweyatCount = c.Tasweyat.Count(t => t.IsPaid),
                Moqaysat = c.Moqaysat.Select(m => new CustomerMoqaysaSummaryViewModel
                {
                    MoqaysaId = m.MoqaysaId,
                    JobOrderNumber = m.JobOrderNumber,
                    FinalGrandTotal = m.FinalGrandTotal,
                    IsIncludedInTasweya = m.IsIncludedInTasweya,
                    IsPaid = m.IsPaid,
                    CreatedAtUtc = m.CreatedAtUtc
                }).ToList(),
                Tasweyat = c.Tasweyat.Select(t => new CustomerTasweyaSummaryViewModel
                {
                    TasweyaId = t.TasweyaId,
                    SettlementNumber = t.SettlementNumber,
                    ClaimNumber = t.ClaimNumber,
                    SettlementGrandTotal = t.SettlementGrandTotal,
                    IsPaid = t.IsPaid,
                    PaidAtUtc = t.PaidAtUtc,
                    SettlementDate = t.SettlementDate
                }).ToList()
            };
        }).ToList();
    }

    public async Task<MoqaysatDetailedReportViewModel> GetMoqaysatDetailedAsync(MoqaysatDetailedFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.Moqaysat
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Where(m => m.IsActive);

        if (filter.FromDate.HasValue) query = query.Where(m => m.CreatedAtUtc >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(m => m.CreatedAtUtc <= filter.ToDate.Value.AddDays(1));
        if (filter.CustomerId.HasValue) query = query.Where(m => m.CustomerId == filter.CustomerId.Value);

        var list = await query.OrderByDescending(m => m.MoqaysaId).ToListAsync(ct);

        var totals = new MoqaysatDetailedTotalsViewModel
        {
            Count = list.Count,
            TotalRawMaterials = MoqaysaCalculationService.Round2(list.Sum(m => m.RawMaterials)),
            TotalMaterialProcessingFee = MoqaysaCalculationService.Round2(list.Sum(m => m.MaterialProcessingFee)),
            TotalMaterials = MoqaysaCalculationService.Round2(list.Sum(m => m.TotalMaterials)),
            TotalWorkerWages = MoqaysaCalculationService.Round2(list.Sum(m => m.TotalWorkerWages)),
            TotalMachineCosts = MoqaysaCalculationService.Round2(list.Sum(m => m.TotalMachineCosts)),
            TotalAdminExpenses = MoqaysaCalculationService.Round2(list.Sum(m => m.AdminExpenses)),
            TotalMarkupAmount = MoqaysaCalculationService.Round2(list.Sum(m => m.MarkupAmount)),
            TotalMinistryOfFinanceFee = MoqaysaCalculationService.Round2(list.Sum(m => m.MinistryOfFinanceFee)),
            TotalWorkersSyndicateFund = MoqaysaCalculationService.Round2(list.Sum(m => m.WorkersSyndicateFund)),
            TotalOtherAmount = MoqaysaCalculationService.Round2(list.Sum(m => m.OtherAmount)),
            TotalVatAmount = MoqaysaCalculationService.Round2(list.Sum(m => m.VatAmount)),
            TotalFinalGrandTotal = MoqaysaCalculationService.Round2(list.Sum(m => m.FinalGrandTotal)),
            FinalGrandTotal = MoqaysaCalculationService.Round2(list.Sum(m => m.FinalGrandTotal))
        };

        var items = list.Select(m => new MoqaysaDetailedItemViewModel
        {
            MoqaysaId = m.MoqaysaId,
            JobOrderNumber = m.JobOrderNumber,
            CustomerNameAr = m.Customer?.CustomerNameAr ?? "غير محدد",
            AccountNameAr = m.Customer?.Account?.AccountNameAr ?? "",
            RawMaterials = m.RawMaterials,
            MaterialProcessingFee = m.MaterialProcessingFee,
            TotalMaterials = m.TotalMaterials,
            TotalWorkerWages = m.TotalWorkerWages,
            TotalMachineCosts = m.TotalMachineCosts,
            AdminExpenses = m.AdminExpenses,
            MarkupAmount = m.MarkupAmount,
            MinistryOfFinanceFee = m.MinistryOfFinanceFee,
            WorkersSyndicateFund = m.WorkersSyndicateFund,
            OtherAmount = m.OtherAmount,
            VatAmount = m.VatAmount,
            FinalGrandTotal = m.FinalGrandTotal,
            IsIncludedInTasweya = m.IsIncludedInTasweya,
            IsPaid = m.IsPaid,
            CreatedAtUtc = m.CreatedAtUtc
        }).ToList();

        return new MoqaysatDetailedReportViewModel
        {
            Totals = totals,
            Items = items
        };
    }

    public async Task<List<PaymentOrderRegisterItemViewModel>> GetPaymentOrdersRegisterAsync(PaymentOrdersRegisterFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .Where(p => p.IsActive);

        if (filter.AccountId.HasValue) query = query.Where(p => p.AccountId == filter.AccountId.Value);
        if (filter.IsConfirmed.HasValue) query = query.Where(p => p.IsConfirmed == filter.IsConfirmed.Value);

        var list = await query.OrderByDescending(p => p.PaymentOrderId).ToListAsync(ct);

        return list.Select(p => new PaymentOrderRegisterItemViewModel
        {
            PaymentOrderId = p.PaymentOrderId,
            PaymentOrderNumber = p.PaymentOrderNumber,
            PaymentDate = p.PaymentDate,
            AccountNameAr = p.Account?.AccountNameAr ?? "غير محدد",
            CustomerNameAr = p.Customer?.CustomerNameAr ?? "مجموعة عملاء",
            TotalPaidAmount = p.TotalPaidAmount,
            TotalAmountInWordsAr = p.TotalAmountInWordsAr,
            IsConfirmed = p.IsConfirmed,
            ConfirmedAtUtc = p.ConfirmedAtUtc,
            Notes = p.Notes,
            Tasweyat = p.TasweyaLinks.Select(l => new PaymentOrderTasweyaLinkViewModel
            {
                PaymentLinkId = l.PaymentLinkId,
                PaymentOrderId = l.PaymentOrderId,
                TasweyaId = l.TasweyaId,
                SettlementNumber = l.Tasweya?.SettlementNumber ?? string.Empty,
                SettlementDate = l.Tasweya?.SettlementDate ?? default,
                ClaimNumber = l.Tasweya?.ClaimNumber ?? string.Empty,
                CustomerNameAr = l.Tasweya?.Customer?.CustomerNameAr ?? string.Empty,
                JobOrderNumbersText = l.Tasweya?.JobOrderNumbersText ?? string.Empty,
                SnapshotTasweyaAmount = l.SnapshotTasweyaAmount,
                IsActive = l.IsActive
            }).ToList()
        }).ToList();
    }
}
