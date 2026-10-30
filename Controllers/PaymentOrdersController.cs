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
[Route("api/payment-orders")]
[Authorize]
public class PaymentOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;

    public PaymentOrdersController(
        ApplicationDbContext context,
        WordExportService wordExportService,
        PdfExportService pdfExportService)
    {
        _context = context;
        _wordExportService = wordExportService;
        _pdfExportService = pdfExportService;
    }

    public record CreatePaymentOrderDto(
        string PaymentOrderNumber,
        DateOnly PaymentDate,
        List<int> TasweyaIds,
        string? Notes,
        bool ConfirmImmediately
    );

    [HttpGet("unpaid-tasweyat")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<IActionResult> GetUnpaidTasweyat([FromQuery] int? accountId, [FromQuery] int? customerId)
    {
        var query = _context.Tasweyat
            .Include(t => t.Account)
            .Include(t => t.Customer)
            .Where(t => t.IsActive && !t.IsPaid);

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        var list = await query
            .OrderByDescending(t => t.TasweyaId)
            .Select(t => new
            {
                t.TasweyaId,
                t.SettlementNumber,
                t.SettlementDate,
                t.ClaimNumber,
                t.AccountId,
                AccountNameAr = t.Account != null ? t.Account.AccountNameAr : "غير محدد",
                t.CustomerId,
                CustomerNameAr = t.Customer != null ? t.Customer.CustomerNameAr : "غير محدد",
                t.JobOrderNumbersText,
                t.SettlementGrandTotal,
                t.TotalAmountInWordsAr,
                t.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] int? customerId,
        [FromQuery] bool? isConfirmed,
        [FromQuery] bool activeOnly = false)
    {
        var query = _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .AsQueryable();

        if (accountId.HasValue) query = query.Where(p => p.AccountId == accountId.Value);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        if (isConfirmed.HasValue) query = query.Where(p => p.IsConfirmed == isConfirmed.Value);
        if (activeOnly) query = query.Where(p => p.IsActive);

        var list = await query
            .OrderByDescending(p => p.PaymentOrderId)
            .Select(p => new
            {
                p.PaymentOrderId,
                p.PaymentOrderNumber,
                p.PaymentDate,
                p.AccountId,
                AccountNameAr = p.Account != null ? p.Account.AccountNameAr : "غير محدد",
                p.CustomerId,
                CustomerNameAr = p.Customer != null ? p.Customer.CustomerNameAr : "مجموعة عملاء",
                p.TotalPaidAmount,
                p.TotalAmountInWordsAr,
                p.IsConfirmed,
                p.ConfirmedAtUtc,
                p.ConfirmedByUserId,
                p.Notes,
                p.IsActive,
                p.CreatedAtUtc,
                TasweyatCount = p.TasweyaLinks.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.Customer)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id);

        if (item == null) return NotFound(new { message = "أمر الدفع غير موجود" });

        return Ok(item);
    }

    [HttpPost]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PaymentOrderNumber))
            return BadRequest(new { message = "يرجى كتابة رقم أمر الدفع الرسمي" });

        if (dto.TasweyaIds == null || dto.TasweyaIds.Count == 0)
            return BadRequest(new { message = "يرجى تحديد كشف تسوية واحد على الأقل لسداده" });

        if (await _context.PaymentOrders.AnyAsync(p => p.PaymentOrderNumber == dto.PaymentOrderNumber.Trim()))
            return BadRequest(new { message = "رقم أمر الدفع مسجل مسبقاً" });

        var tasweyat = await _context.Tasweyat
            .Include(t => t.MoqaysaLinks)
                .ThenInclude(ml => ml.Moqaysa)
            .Where(t => dto.TasweyaIds.Contains(t.TasweyaId) && t.IsActive)
            .ToListAsync();

        if (tasweyat.Count != dto.TasweyaIds.Count)
            return BadRequest(new { message = "بعض كشوف التسوية المحددة غير موجودة أو غير فعالة" });

        if (tasweyat.Any(t => t.IsPaid))
            return BadRequest(new { message = "إحدى كشوف التسوية المحددة تم سدادها بالفعل مسبقاً" });

        // Account & customer inference
        int accountId = tasweyat.First().AccountId;
        int? customerId = tasweyat.Select(t => t.CustomerId).Distinct().Count() == 1 ? tasweyat.First().CustomerId : null;

        decimal totalPaidAmount = MoqaysaCalculationService.Round2(tasweyat.Sum(t => t.SettlementGrandTotal));
        string tafqeet = ArabicTafqeetService.ToWords(totalPaidAmount);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var paymentOrder = new PaymentOrder
        {
            PaymentOrderNumber = dto.PaymentOrderNumber.Trim(),
            PaymentDate = dto.PaymentDate,
            AccountId = accountId,
            CustomerId = customerId,
            TotalPaidAmount = totalPaidAmount,
            TotalAmountInWordsAr = tafqeet,
            Notes = dto.Notes?.Trim(),
            IsConfirmed = false,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        foreach (var t in tasweyat)
        {
            paymentOrder.TasweyaLinks.Add(new PaymentOrderTasweyaLink
            {
                TasweyaId = t.TasweyaId,
                SnapshotTasweyaAmount = t.SettlementGrandTotal
            });
        }

        if (dto.ConfirmImmediately)
        {
            paymentOrder.IsConfirmed = true;
            paymentOrder.ConfirmedAtUtc = DateTime.UtcNow;
            paymentOrder.ConfirmedByUserId = currentUserId;

            foreach (var t in tasweyat)
            {
                t.IsPaid = true;
                t.PaidAtUtc = DateTime.UtcNow;

                foreach (var ml in t.MoqaysaLinks)
                {
                    if (ml.Moqaysa != null)
                    {
                        ml.Moqaysa.IsPaid = true;
                        ml.Moqaysa.PaidAtUtc = DateTime.UtcNow;
                    }
                }
            }
        }

        _context.PaymentOrders.Add(paymentOrder);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = paymentOrder.IsConfirmed
                ? "تم إصدار وتأكيد أمر الدفع واعتماد السداد رسمياً بنجاح"
                : "تم حفظ أمر الدفع كمسودة (بانتظار التأكيد للاعتماد)",
            paymentOrderId = paymentOrder.PaymentOrderId,
            isConfirmed = paymentOrder.IsConfirmed,
            totalPaidAmount
        });
    }

    [HttpPost("{id}/confirm")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> ConfirmPaymentOrder(int id)
    {
        var paymentOrder = await _context.PaymentOrders
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.MoqaysaLinks)
                        .ThenInclude(ml => ml.Moqaysa)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id);

        if (paymentOrder == null) return NotFound(new { message = "أمر الدفع غير موجود" });

        if (paymentOrder.IsConfirmed)
            return BadRequest(new { message = "أمر الدفع مؤكد ومسدد بالفعل مسبقاً" });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.UtcNow;

        paymentOrder.IsConfirmed = true;
        paymentOrder.ConfirmedAtUtc = now;
        paymentOrder.ConfirmedByUserId = currentUserId;

        foreach (var link in paymentOrder.TasweyaLinks)
        {
            if (link.Tasweya != null)
            {
                link.Tasweya.IsPaid = true;
                link.Tasweya.PaidAtUtc = now;

                foreach (var moqaysaLink in link.Tasweya.MoqaysaLinks)
                {
                    if (moqaysaLink.Moqaysa != null)
                    {
                        moqaysaLink.Moqaysa.IsPaid = true;
                        moqaysaLink.Moqaysa.PaidAtUtc = now;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "تم اعتماد وتأكيد أمر الدفع بنجاح، وتحولت كافة كشوف التسوية والمقايسات المرتبطة به إلى مسددة رسمياً",
            isConfirmed = true,
            confirmedAtUtc = now
        });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.PaymentOrders.FindAsync(id);
        if (item == null) return NotFound(new { message = "أمر الدفع غير موجود" });

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = item.IsActive ? "تم تفعيل أمر الدفع" : "تم إلغاء تفعيل أمر الدفع", isActive = item.IsActive });
    }

    [HttpGet("{id}/export-word")]
    public async Task<IActionResult> ExportWord(int id)
    {
        var item = await _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id);

        if (item == null) return NotFound();

        byte[] bytes = _wordExportService.ExportPaymentOrderToWord(item);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"PaymentOrder_{item.PaymentOrderNumber}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var item = await _context.PaymentOrders
            .Include(p => p.Account)
            .Include(p => p.TasweyaLinks)
                .ThenInclude(l => l.Tasweya)
                    .ThenInclude(t => t!.Customer)
            .FirstOrDefaultAsync(p => p.PaymentOrderId == id);

        if (item == null) return NotFound();

        byte[] bytes = _pdfExportService.ExportPaymentOrderToPdf(item);
        return File(bytes, "application/pdf", $"PaymentOrder_{item.PaymentOrderNumber}.pdf");
    }
}
