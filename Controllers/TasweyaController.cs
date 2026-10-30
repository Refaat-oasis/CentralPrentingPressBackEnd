using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasweyaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;

    public TasweyaController(
        ApplicationDbContext context,
        WordExportService wordExportService,
        PdfExportService pdfExportService)
    {
        _context = context;
        _wordExportService = wordExportService;
        _pdfExportService = pdfExportService;
    }

    public record CreateTasweyaDto(
        string SettlementNumber,
        DateOnly SettlementDate,
        int AccountId,
        int CustomerId,
        string ClaimNumber,
        List<int> MoqaysaIds,
        decimal? OverrideMarkupPercent = null
    );

    [HttpGet("unsettled-moqaysat")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<IActionResult> GetUnsettledMoqaysat([FromQuery] int? customerId, [FromQuery] int? accountId)
    {
        var query = _context.Moqaysat
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .Where(m => m.IsActive && !m.IsIncludedInTasweya);

        if (customerId.HasValue)
            query = query.Where(m => m.CustomerId == customerId.Value);

        if (accountId.HasValue)
            query = query.Where(m => m.Customer != null && m.Customer.AccountId == accountId.Value);

        var list = await query
            .OrderByDescending(m => m.MoqaysaId)
            .Select(m => new
            {
                m.MoqaysaId,
                m.JobOrderNumber,
                m.CustomerId,
                CustomerNameAr = m.Customer != null ? m.Customer.CustomerNameAr : "غير محدد",
                AccountId = m.Customer != null ? m.Customer.AccountId : 0,
                AccountNameAr = m.Customer != null && m.Customer.Account != null ? m.Customer.Account.AccountNameAr : "",
                m.RequiredQuantityText,
                m.FinalGrandTotal,
                m.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] int? customerId,
        [FromQuery] bool? isPaid,
        [FromQuery] bool activeOnly = false)
    {
        var query = _context.Tasweyat
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .AsQueryable();

        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (customerId.HasValue) query = query.Where(t => t.CustomerId == customerId.Value);
        if (isPaid.HasValue) query = query.Where(t => t.IsPaid == isPaid.Value);
        if (activeOnly) query = query.Where(t => t.IsActive);

        var list = await query
            .OrderByDescending(t => t.TasweyaId)
            .Select(t => new
            {
                t.TasweyaId,
                t.SettlementNumber,
                t.SettlementDate,
                t.AccountId,
                AccountNameAr = t.Account != null ? t.Account.AccountNameAr : "غير محدد",
                t.CustomerId,
                CustomerNameAr = t.Customer != null ? t.Customer.CustomerNameAr : "غير محدد",
                t.ClaimNumber,
                t.JobOrderNumbersText,
                t.SettlementGrandTotal,
                t.TotalAmountInWordsAr,
                t.IsPaid,
                t.PaidAtUtc,
                t.IsActive,
                t.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _context.Tasweyat
            .Include(x => x.Account)
            .Include(x => x.Customer)
            .Include(x => x.MoqaysaLinks)
                .ThenInclude(l => l.Moqaysa)
            .FirstOrDefaultAsync(x => x.TasweyaId == id);

        if (t == null) return NotFound(new { message = "كشف التسوية غير موجود" });

        return Ok(new
        {
            t.TasweyaId,
            t.SettlementNumber,
            t.SettlementDate,
            t.AccountId,
            Account = t.Account != null ? new
            {
                t.Account.AccountId,
                t.Account.AccountNameAr,
                t.Account.AccountCode,
                t.Account.DebitHeaderTextAr
            } : null,
            t.CustomerId,
            Customer = t.Customer != null ? new
            {
                t.Customer.CustomerId,
                t.Customer.CustomerNameAr
            } : null,
            t.ClaimNumber,
            t.JobOrderNumbersText,
            t.TotalMaterialsSum,
            t.TotalWorkerWagesSum,
            t.TotalMachineCostsSum,
            t.AdminExpensesSum,
            t.MarkupAmountSum,
            t.MinistryOfFinanceSum,
            t.WorkersFundSum,
            t.OtherAmountSum,
            t.RevenuesTotal,
            t.VatTotal,
            t.SettlementGrandTotal,
            t.TotalAmountInWordsAr,
            t.IsPaid,
            t.PaidAtUtc,
            t.IsActive,
            t.CreatedAtUtc,
            MoqaysaLinks = t.MoqaysaLinks.Select(link => new
            {
                link.LinkId,
                link.MoqaysaId,
                Moqaysa = link.Moqaysa != null ? new
                {
                    link.Moqaysa.MoqaysaId,
                    link.Moqaysa.JobOrderNumber,
                    link.Moqaysa.FinalGrandTotal
                } : null
            }).ToList()
        });
    }

    [HttpPost]
    [PermissionAuthorize("TASWEYA", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] CreateTasweyaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SettlementNumber))
            return BadRequest(new { message = "يرجى كتابة رقم التسوية" });

        if (dto.MoqaysaIds == null || dto.MoqaysaIds.Count == 0)
            return BadRequest(new { message = "يرجى تحديد مقايسة واحدة على الأقل لإدراجها في كشف التسوية" });

        var account = await _context.Accounts.FindAsync(dto.AccountId);
        if (account == null)
            return BadRequest(new { message = "الحساب المختار غير صحيح" });

        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest(new { message = "الجهة الطالبة غير موجودة" });

        // Load the moqaysat
        var moqaysat = await _context.Moqaysat
            .Where(m => dto.MoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync();

        if (moqaysat.Count != dto.MoqaysaIds.Count)
            return BadRequest(new { message = "بعض المقايسات المحددة غير موجودة أو غير فعالة" });

        if (moqaysat.Any(m => m.IsIncludedInTasweya))
            return BadRequest(new { message = "إحدى المقايسات المحددة مُدرجة بالفعل في كشف تسوية آخر" });

        // Aggregate financial cascade
        decimal totalMat = moqaysat.Sum(m => m.TotalMaterials);
        decimal totalWages = moqaysat.Sum(m => m.TotalWorkerWages);
        decimal totalMach = moqaysat.Sum(m => m.TotalMachineCosts);
        decimal totalAdmin = moqaysat.Sum(m => m.AdminExpenses);
        decimal totalMarkup = moqaysat.Sum(m => m.MarkupAmount);

        // Allow user to choose which percent to apply in the Tasweya
        if (dto.OverrideMarkupPercent.HasValue && dto.OverrideMarkupPercent.Value >= 0)
        {
            decimal baseExpenses = totalMat + totalWages + totalMach + totalAdmin;
            totalMarkup = MoqaysaCalculationService.Round2(baseExpenses * (dto.OverrideMarkupPercent.Value / 100m));
        }

        decimal totalMof = moqaysat.Sum(m => m.MinistryOfFinanceFee);
        decimal totalWorkers = moqaysat.Sum(m => m.WorkersSyndicateFund);
        decimal totalOther = moqaysat.Sum(m => m.OtherAmount);

        decimal revenuesTotal = MoqaysaCalculationService.Round2(
            totalMat + totalWages + totalMach + totalAdmin + totalMarkup + totalMof + totalWorkers + totalOther);

        decimal vatTotal = MoqaysaCalculationService.Round2(
            moqaysat.Any(m => m.ApplyVat) ? revenuesTotal * 0.14m : moqaysat.Sum(m => m.VatAmount));
        decimal grandTotal = MoqaysaCalculationService.Round2(revenuesTotal + vatTotal);

        string tafqeet = ArabicTafqeetService.ToWords(grandTotal);
        string jobOrders = string.Join(" , ", moqaysat.Select(m => m.JobOrderNumber).Distinct());
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var tasweya = new Tasweya
        {
            SettlementNumber = dto.SettlementNumber.Trim(),
            SettlementDate = dto.SettlementDate,
            AccountId = dto.AccountId,
            CustomerId = dto.CustomerId,
            ClaimNumber = string.IsNullOrWhiteSpace(dto.ClaimNumber) ? dto.SettlementNumber.Trim() : dto.ClaimNumber.Trim(),
            JobOrderNumbersText = jobOrders,

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
            SettlementGrandTotal = grandTotal,
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
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم إصدار كشف التسوية (المطالبة المالية) بنجاح", tasweyaId = tasweya.TasweyaId });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("TASWEYA", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.Tasweyat.FindAsync(id);
        if (item == null) return NotFound(new { message = "كشف التسوية غير موجود" });

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = item.IsActive ? "تم تفعيل كشف التسوية" : "تم إلغاء تفعيل كشف التسوية", isActive = item.IsActive });
    }

    [HttpGet("{id}/export-word")]
    public async Task<IActionResult> ExportWord(int id)
    {
        var item = await _context.Tasweyat
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TasweyaId == id);

        if (item == null) return NotFound();

        byte[] bytes = _wordExportService.ExportTasweyaToWord(item);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Tasweya_{item.SettlementNumber}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var item = await _context.Tasweyat
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TasweyaId == id);

        if (item == null) return NotFound();

        byte[] bytes = _pdfExportService.ExportTasweyaToPdf(item);
        return File(bytes, "application/pdf", $"Tasweya_{item.SettlementNumber}.pdf");
    }
}
