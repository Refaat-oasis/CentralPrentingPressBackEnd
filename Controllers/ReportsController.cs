using System;
using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("accounts-summary")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<IActionResult> GetAccountsSummary()
    {
        var accounts = await _context.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountId)
            .ToListAsync();

        var moqaysat = await _context.Moqaysat
            .Include(m => m.Customer)
            .Where(m => m.IsActive && m.Customer != null)
            .ToListAsync();

        var tasweyat = await _context.Tasweyat
            .Where(t => t.IsActive)
            .ToListAsync();

        var result = accounts.Select(acc =>
        {
            var accMoqaysat = moqaysat.Where(m => m.Customer!.AccountId == acc.AccountId).ToList();
            var accTasweyat = tasweyat.Where(t => t.AccountId == acc.AccountId).ToList();

            decimal totalBilled = MoqaysaCalculationService.Round2(accMoqaysat.Sum(m => m.FinalGrandTotal));
            decimal totalClaimed = MoqaysaCalculationService.Round2(accTasweyat.Sum(t => t.SettlementGrandTotal));
            decimal totalPaid = MoqaysaCalculationService.Round2(accTasweyat.Where(t => t.IsPaid).Sum(t => t.SettlementGrandTotal));

            decimal unpaidClaimsInTasweya = MoqaysaCalculationService.Round2(accTasweyat.Where(t => !t.IsPaid).Sum(t => t.SettlementGrandTotal));
            decimal unsettledMoqaysat = MoqaysaCalculationService.Round2(accMoqaysat.Where(m => !m.IsIncludedInTasweya).Sum(m => m.FinalGrandTotal));
            decimal totalUnpaid = MoqaysaCalculationService.Round2(totalBilled - totalPaid);

            return new
            {
                acc.AccountId,
                acc.AccountCode,
                acc.AccountNameAr,
                acc.DebitHeaderTextAr,
                TotalBilledAmount = totalBilled,
                TotalClaimedInTasweya = totalClaimed,
                TotalPaidAmount = totalPaid,
                TotalUnpaidAmount = totalUnpaid,
                UnpaidClaimsInTasweya = unpaidClaimsInTasweya,
                UnsettledMoqaysat = unsettledMoqaysat,
                MoqaysatCount = accMoqaysat.Count,
                TasweyatCount = accTasweyat.Count,
                PaidTasweyatCount = accTasweyat.Count(t => t.IsPaid)
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("customers-summary")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<IActionResult> GetCustomersSummary([FromQuery] int? customerId, [FromQuery] int? accountId)
    {
        var query = _context.Customers
            .Include(c => c.Account)
            .Include(c => c.Moqaysat.Where(m => m.IsActive))
            .Include(c => c.Tasweyat.Where(t => t.IsActive))
            .Where(c => c.IsActive);

        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);
        if (accountId.HasValue) query = query.Where(c => c.AccountId == accountId.Value);

        var customers = await query.OrderBy(c => c.CustomerNameAr).ToListAsync();

        var result = customers.Select(c =>
        {
            decimal totalBilled = MoqaysaCalculationService.Round2(c.Moqaysat.Sum(m => m.FinalGrandTotal));
            decimal totalPaid = MoqaysaCalculationService.Round2(c.Tasweyat.Where(t => t.IsPaid).Sum(t => t.SettlementGrandTotal));
            decimal totalUnpaid = MoqaysaCalculationService.Round2(totalBilled - totalPaid);

            return new
            {
                c.CustomerId,
                c.CustomerNameAr,
                c.AccountId,
                AccountNameAr = c.Account?.AccountNameAr ?? "غير محدد",
                TotalBilledAmount = totalBilled,
                TotalPaidAmount = totalPaid,
                TotalUnpaidAmount = totalUnpaid,
                MoqaysatCount = c.Moqaysat.Count,
                TasweyatCount = c.Tasweyat.Count,
                PaidTasweyatCount = c.Tasweyat.Count(t => t.IsPaid),
                Moqaysat = c.Moqaysat.Select(m => new
                {
                    m.MoqaysaId,
                    m.JobOrderNumber,
                    m.FinalGrandTotal,
                    m.IsIncludedInTasweya,
                    m.IsPaid,
                    m.CreatedAtUtc
                }),
                Tasweyat = c.Tasweyat.Select(t => new
                {
                    t.TasweyaId,
                    t.SettlementNumber,
                    t.ClaimNumber,
                    t.SettlementGrandTotal,
                    t.IsPaid,
                    t.PaidAtUtc,
                    t.SettlementDate
                })
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("moqaysat-detailed")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<IActionResult> GetMoqaysatDetailed(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? customerId)
    {
        var query = _context.Moqaysat
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Where(m => m.IsActive);

        if (fromDate.HasValue) query = query.Where(m => m.CreatedAtUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAtUtc <= toDate.Value.AddDays(1));
        if (customerId.HasValue) query = query.Where(m => m.CustomerId == customerId.Value);

        var list = await query.OrderByDescending(m => m.MoqaysaId).ToListAsync();

        var totals = new
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
            FinalGrandTotal = MoqaysaCalculationService.Round2(list.Sum(m => m.FinalGrandTotal))
        };

        return Ok(new
        {
            totals,
            items = list.Select(m => new
            {
                m.MoqaysaId,
                m.JobOrderNumber,
                CustomerNameAr = m.Customer?.CustomerNameAr ?? "غير محدد",
                AccountNameAr = m.Customer?.Account?.AccountNameAr ?? "",
                m.RawMaterials,
                m.MaterialProcessingFee,
                m.TotalMaterials,
                m.TotalWorkerWages,
                m.TotalMachineCosts,
                m.AdminExpenses,
                m.MarkupAmount,
                m.MinistryOfFinanceFee,
                m.WorkersSyndicateFund,
                m.OtherAmount,
                m.VatAmount,
                m.FinalGrandTotal,
                m.IsIncludedInTasweya,
                m.IsPaid,
                m.CreatedAtUtc
            })
        });
    }

    [HttpGet("payment-orders-register")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<IActionResult> GetPaymentOrdersRegister(
        [FromQuery] int? accountId,
        [FromQuery] bool? isConfirmed)
    {
        var query = _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .Where(p => p.IsActive);

        if (accountId.HasValue) query = query.Where(p => p.AccountId == accountId.Value);
        if (isConfirmed.HasValue) query = query.Where(p => p.IsConfirmed == isConfirmed.Value);

        var list = await query.OrderByDescending(p => p.PaymentOrderId).ToListAsync();

        var result = list.Select(p => new
        {
            p.PaymentOrderId,
            p.PaymentOrderNumber,
            p.PaymentDate,
            AccountNameAr = p.Account?.AccountNameAr ?? "غير محدد",
            CustomerNameAr = p.Customer?.CustomerNameAr ?? "مجموعة عملاء",
            p.TotalPaidAmount,
            p.TotalAmountInWordsAr,
            p.IsConfirmed,
            p.ConfirmedAtUtc,
            p.Notes,
            Tasweyat = p.TasweyaLinks.Select(l => new
            {
                l.TasweyaId,
                l.Tasweya?.SettlementNumber,
                l.Tasweya?.ClaimNumber,
                CustomerNameAr = l.Tasweya?.Customer?.CustomerNameAr ?? "",
                l.SnapshotTasweyaAmount
            })
        });

        return Ok(result);
    }
}
