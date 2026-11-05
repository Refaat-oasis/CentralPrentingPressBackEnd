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
            .Where(m => m.IsActive && (!m.IsIncludedInTasweya || !_context.TasweyaMoqaysaLinks.Any(link => link.MoqaysaId == m.MoqaysaId && link.Tasweya != null && link.Tasweya.IsActive)));

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
                m.RawMaterials,
                m.MaterialProcessingFee,
                m.FilmsProcessingFee,
                m.TotalMaterials,
                m.TotalWorkerWages,
                m.TotalMachineCosts,
                m.AdminExpenses,
                m.MarkupAmount,
                m.ApplyMarkup,
                m.SelectedMarkupPercent,
                m.MinistryOfFinanceFee,
                m.WorkersSyndicateFund,
                m.OtherAmount,
                m.ApplyVat,
                m.VatAmount,
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

        if (string.IsNullOrWhiteSpace(dto.ClaimNumber) || !System.Text.RegularExpressions.Regex.IsMatch(dto.ClaimNumber.Trim(), @"^\d+$"))
            return BadRequest(new { message = "رقم المطالبة يجب أن يتكون من أرقام فقط" });

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

        if (moqaysat.Any(m => m.CustomerId != dto.CustomerId))
            return BadRequest(new { message = "جميع المقايسات المحددة يجب أن تتبع نفس الجهة الطالبة لكشف التسوية" });

        var activeConflicts = await _context.TasweyaMoqaysaLinks
            .Include(link => link.Moqaysa)
            .Where(link => dto.MoqaysaIds.Contains(link.MoqaysaId) && link.Tasweya != null && link.Tasweya.IsActive)
            .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
            .Where(jobNo => !string.IsNullOrEmpty(jobNo))
            .Distinct()
            .ToListAsync();

        if (activeConflicts.Count > 0)
            return BadRequest(new { message = $"إحدى المقايسات المحددة (إذن تشغيل: {string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط" });

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
            ClaimNumber = dto.ClaimNumber.Trim(),
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

    [HttpPut("{id}")]
    [PermissionAuthorize("TASWEYA", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTasweyaDto dto)
    {
        var tasweya = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
            .FirstOrDefaultAsync(t => t.TasweyaId == id);

        if (tasweya == null) return NotFound(new { message = "كشف التسوية غير موجود" });

        if (tasweya.IsPaid)
            return BadRequest(new { message = "لا يمكن تعديل كشف تسوية تم سداده وتأكيده بأمر دفع رسمي" });

        if (string.IsNullOrWhiteSpace(dto.SettlementNumber))
            return BadRequest(new { message = "يرجى كتابة رقم التسوية" });

        if (dto.MoqaysaIds == null || dto.MoqaysaIds.Count == 0)
            return BadRequest(new { message = "يرجى تحديد مقايسة واحدة على الأقل" });

        var account = await _context.Accounts.FindAsync(dto.AccountId);
        if (account == null) return BadRequest(new { message = "الحساب المختار غير صحيح" });

        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null) return BadRequest(new { message = "الجهة الطالبة غير موجودة" });

        // Release old moqaysat
        var oldMoqaysaIds = tasweya.MoqaysaLinks.Select(ml => ml.MoqaysaId).ToList();
        var oldMoqaysat = await _context.Moqaysat.Where(m => oldMoqaysaIds.Contains(m.MoqaysaId)).ToListAsync();
        foreach (var om in oldMoqaysat)
        {
            om.IsIncludedInTasweya = false;
        }
        _context.TasweyaMoqaysaLinks.RemoveRange(tasweya.MoqaysaLinks);

        // Load new moqaysat
        var moqaysat = await _context.Moqaysat
            .Where(m => dto.MoqaysaIds.Contains(m.MoqaysaId) && m.IsActive)
            .ToListAsync();

        if (moqaysat.Count != dto.MoqaysaIds.Count)
            return BadRequest(new { message = "بعض المقايسات المحددة غير موجودة أو غير فعالة" });

        if (moqaysat.Any(m => m.CustomerId != dto.CustomerId))
            return BadRequest(new { message = "جميع المقايسات المحددة يجب أن تتبع نفس الجهة الطالبة لكشف التسوية" });

        var activeConflicts = await _context.TasweyaMoqaysaLinks
            .Include(link => link.Moqaysa)
            .Where(link => dto.MoqaysaIds.Contains(link.MoqaysaId) && link.TasweyaId != id && link.Tasweya != null && link.Tasweya.IsActive)
            .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
            .Where(jobNo => !string.IsNullOrEmpty(jobNo))
            .Distinct()
            .ToListAsync();

        if (activeConflicts.Count > 0)
            return BadRequest(new { message = $"إحدى المقايسات المحددة (إذن تشغيل: {string.Join(" ، ", activeConflicts)}) مُدرجة بالفعل في كشف تسوية آخر نشط" });

        // Aggregate financial cascade
        decimal totalMat = moqaysat.Sum(m => m.TotalMaterials);
        decimal totalWages = moqaysat.Sum(m => m.TotalWorkerWages);
        decimal totalMach = moqaysat.Sum(m => m.TotalMachineCosts);
        decimal totalAdmin = moqaysat.Sum(m => m.AdminExpenses);
        decimal totalMarkup = moqaysat.Sum(m => m.MarkupAmount);

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

        tasweya.SettlementNumber = dto.SettlementNumber.Trim();
        tasweya.SettlementDate = dto.SettlementDate;
        tasweya.AccountId = dto.AccountId;
        tasweya.CustomerId = dto.CustomerId;
        tasweya.ClaimNumber = string.IsNullOrWhiteSpace(dto.ClaimNumber) ? dto.SettlementNumber.Trim() : dto.ClaimNumber.Trim();
        tasweya.JobOrderNumbersText = jobOrders;

        tasweya.TotalMaterialsSum = totalMat;
        tasweya.TotalWorkerWagesSum = totalWages;
        tasweya.TotalMachineCostsSum = totalMach;
        tasweya.AdminExpensesSum = totalAdmin;
        tasweya.MarkupAmountSum = totalMarkup;
        tasweya.MinistryOfFinanceSum = totalMof;
        tasweya.WorkersFundSum = totalWorkers;
        tasweya.OtherAmountSum = totalOther;

        tasweya.RevenuesTotal = revenuesTotal;
        tasweya.VatTotal = vatTotal;
        tasweya.SettlementGrandTotal = grandTotal;
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

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تعديل كشف التسوية وحفظ التحديثات بنجاح", tasweyaId = tasweya.TasweyaId });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("TASWEYA", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
            .FirstOrDefaultAsync(t => t.TasweyaId == id);

        if (item == null) return NotFound(new { message = "كشف التسوية غير موجود" });

        var moqaysaIds = item.MoqaysaLinks.Select(ml => ml.MoqaysaId).ToList();
        var linkedMoqaysat = await _context.Moqaysat.Where(m => moqaysaIds.Contains(m.MoqaysaId)).ToListAsync();

        if (item.IsActive)
        {
            // Deactivating: Release all linked moqaysat so they become unsettled and can be added to new tasweyat
            if (item.IsPaid)
            {
                return BadRequest(new { message = "لا يمكن إلغاء تفعيل كشف تسوية تم سداده وتأكيده بأمر دفع رسمي" });
            }

            foreach (var m in linkedMoqaysat)
            {
                m.IsIncludedInTasweya = false;
            }

            item.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إلغاء تفعيل كشف التسوية وتحرير المقايسات التابعة له لتصبح متاحة في كشوف التسوية الجديدة",
                isActive = false
            });
        }
        else
        {
            // Re-activating: Verify none of the linked moqaysat are currently in another ACTIVE tasweya
            var alreadyIncludedInActive = await _context.TasweyaMoqaysaLinks
                .Include(link => link.Moqaysa)
                .Where(link => moqaysaIds.Contains(link.MoqaysaId) && link.TasweyaId != item.TasweyaId && link.Tasweya != null && link.Tasweya.IsActive)
                .Select(link => link.Moqaysa != null ? link.Moqaysa.JobOrderNumber : "")
                .Where(jobNo => !string.IsNullOrEmpty(jobNo))
                .Distinct()
                .ToListAsync();

            if (alreadyIncludedInActive.Count > 0)
            {
                return BadRequest(new
                {
                    message = $"لا يمكن إعادة تفعيل كشف التسوية؛ نظراً لأن المقايسات ذات أذون التشغيل ({string.Join(" ، ", alreadyIncludedInActive)}) تم إدراجها بالفعل في كشف تسوية آخر نشط."
                });
            }

            // Re-lock all linked moqaysat back
            foreach (var m in linkedMoqaysat)
            {
                m.IsIncludedInTasweya = true;
            }

            item.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إعادة تفعيل كشف التسوية وربط المقايسات التابعة له بنجاح",
                isActive = true
            });
        }
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
