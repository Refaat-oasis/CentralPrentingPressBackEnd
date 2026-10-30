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
public class MoqaysaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly MoqaysaCalculationService _calculationService;
    private readonly WordExportService _wordExportService;
    private readonly PdfExportService _pdfExportService;

    public MoqaysaController(
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

    public record MaterialItemInputDto(
        int? MaterialId,
        string MaterialNameSnapshot,
        decimal Quantity,
        decimal UnitPrice
    );

    public record MachineryItemInputDto(
        int MachineryId,
        string MachineryNameSnapshot,
        decimal Hours,
        decimal WorkerHourlyRateSnapshot,
        decimal MachineHourlyRateSnapshot
    );

    public record MoqaysaSaveDto(
        string JobOrderNumber,
        int CustomerId,
        string RequiredQuantityText,
        string SpecificationsText,
        decimal FilmsProcessingFee,
        bool ApplyAdminExpenses,
        bool ApplyMarkup,
        decimal SelectedMarkupPercent,
        bool ApplyOther,
        bool ApplyVat,
        List<MaterialItemInputDto> MaterialItems,
        List<MachineryItemInputDto> MachineryItems
    );

    [HttpPost("calculate-preview")]
    public async Task<IActionResult> CalculatePreview([FromBody] MoqaysaSaveDto dto)
    {
        var result = await PerformCalculationAsync(dto);
        return Ok(result);
    }

    [HttpGet]
    [PermissionAuthorize("MOQAYSA", "CanAccess")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] string? jobOrderNumber,
        [FromQuery] bool? isIncludedInTasweya,
        [FromQuery] bool? isPaid,
        [FromQuery] bool activeOnly = false)
    {
        var query = _context.Moqaysat
            .Include(m => m.Customer)
                .ThenInclude(c => c!.Account)
            .AsQueryable();

        if (customerId.HasValue) query = query.Where(m => m.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(jobOrderNumber)) query = query.Where(m => m.JobOrderNumber.Contains(jobOrderNumber));
        if (isIncludedInTasweya.HasValue) query = query.Where(m => m.IsIncludedInTasweya == isIncludedInTasweya.Value);
        if (isPaid.HasValue) query = query.Where(m => m.IsPaid == isPaid.Value);
        if (activeOnly) query = query.Where(m => m.IsActive);

        var list = await query
            .OrderByDescending(m => m.MoqaysaId)
            .Select(m => new
            {
                m.MoqaysaId,
                m.JobOrderNumber,
                m.CustomerId,
                CustomerNameAr = m.Customer != null ? m.Customer.CustomerNameAr : "غير محدد",
                AccountNameAr = m.Customer != null && m.Customer.Account != null ? m.Customer.Account.AccountNameAr : "",
                m.RequiredQuantityText,
                m.FinalGrandTotal,
                m.IsIncludedInTasweya,
                m.IsPaid,
                m.IsActive,
                m.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("MOQAYSA", "CanAccess")]
    public async Task<IActionResult> GetById(int id)
    {
        var m = await _context.Moqaysat
            .Include(x => x.Customer)
                .ThenInclude(c => c!.Account)
            .Include(x => x.MaterialItems)
            .Include(x => x.MachineryItems)
            .FirstOrDefaultAsync(x => x.MoqaysaId == id);

        if (m == null) return NotFound(new { message = "المقايسة غير موجودة" });

        return Ok(new
        {
            m.MoqaysaId,
            m.JobOrderNumber,
            m.CustomerId,
            Customer = m.Customer != null ? new
            {
                m.Customer.CustomerId,
                m.Customer.CustomerNameAr,
                m.Customer.AccountId,
                Account = m.Customer.Account != null ? new
                {
                    m.Customer.Account.AccountId,
                    m.Customer.Account.AccountNameAr,
                    m.Customer.Account.DebitHeaderTextAr
                } : null
            } : null,
            m.RequiredQuantityText,
            m.SpecificationsText,
            MaterialItems = m.MaterialItems.Select(mat => new
            {
                mat.ItemId,
                mat.MaterialId,
                mat.MaterialNameSnapshot,
                mat.Quantity,
                mat.UnitPrice,
                mat.TotalPrice
            }).ToList(),
            MachineryItems = m.MachineryItems.Select(mac => new
            {
                mac.OperationId,
                mac.MachineryId,
                mac.MachineryNameSnapshot,
                mac.Hours,
                mac.WorkerHourlyRateSnapshot,
                mac.TotalWorkerWages,
                mac.MachineHourlyRateSnapshot,
                mac.TotalMachineOperatingCost
            }).ToList(),
            m.RawMaterials,
            m.MaterialProcessingFee,
            m.FilmsProcessingFee,
            m.TotalMaterials,
            m.TotalWorkerWages,
            m.TotalMachineCosts,
            m.SubTotal1,
            m.ApplyAdminExpenses,
            m.AdminExpenses,
            m.TotalExpenses,
            m.ApplyMarkup,
            m.SelectedMarkupPercent,
            m.MarkupAmount,
            m.TotalAfterMarkup,
            m.MinistryOfFinanceFee,
            m.WorkersSyndicateFund,
            m.SubTotal2,
            m.ApplyOther,
            m.OtherAmount,
            m.SubTotal3,
            m.ApplyVat,
            m.VatAmount,
            m.FinalGrandTotal,
            m.IsIncludedInTasweya,
            m.IsPaid,
            m.IsActive,
            m.CreatedAtUtc
        });
    }

    [HttpPost]
    [PermissionAuthorize("MOQAYSA", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] MoqaysaSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.JobOrderNumber))
            return BadRequest(new { message = "يرجى كتابة رقم إذن التشغيل" });

        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest(new { message = "الجهة الطالبة غير موجودة" });

        var calc = await PerformCalculationAsync(dto);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var moqaysa = new Moqaysa
        {
            JobOrderNumber = dto.JobOrderNumber.Trim(),
            CustomerId = dto.CustomerId,
            RequiredQuantityText = dto.RequiredQuantityText?.Trim() ?? "",
            SpecificationsText = dto.SpecificationsText?.Trim() ?? "",

            RawMaterials = calc.RawMaterials,
            MaterialProcessingFee = calc.MaterialProcessingFee,
            FilmsProcessingFee = calc.FilmsProcessingFee,
            TotalMaterials = calc.TotalMaterials,
            TotalWorkerWages = calc.TotalWorkerWages,
            TotalMachineCosts = calc.TotalMachineCosts,
            SubTotal1 = calc.SubTotal1,
            ApplyAdminExpenses = calc.ApplyAdminExpenses,
            AdminExpenses = calc.AdminExpenses,
            TotalExpenses = calc.TotalExpenses,
            ApplyMarkup = calc.ApplyMarkup,
            SelectedMarkupPercent = calc.SelectedMarkupPercent,
            MarkupAmount = calc.MarkupAmount,
            TotalAfterMarkup = calc.TotalAfterMarkup,
            MinistryOfFinanceFee = calc.MinistryOfFinanceFee,
            WorkersSyndicateFund = calc.WorkersSyndicateFund,
            SubTotal2 = calc.SubTotal2,
            ApplyOther = calc.ApplyOther,
            OtherAmount = calc.OtherAmount,
            SubTotal3 = calc.SubTotal3,
            ApplyVat = calc.ApplyVat,
            VatAmount = calc.VatAmount,
            FinalGrandTotal = calc.FinalGrandTotal,

            IsIncludedInTasweya = false,
            IsPaid = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId,
            IsActive = true
        };

        // Material Items
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

        // Machinery Items
        foreach (var mac in dto.MachineryItems)
        {
            decimal wCost = MoqaysaCalculationService.Round2(mac.Hours * mac.WorkerHourlyRateSnapshot);
            decimal mCost = MoqaysaCalculationService.Round2(mac.Hours * mac.MachineHourlyRateSnapshot);
            moqaysa.MachineryItems.Add(new MoqaysaMachineryItem
            {
                MachineryId = mac.MachineryId,
                MachineryNameSnapshot = mac.MachineryNameSnapshot?.Trim() ?? "آلة",
                Hours = mac.Hours,
                WorkerHourlyRateSnapshot = mac.WorkerHourlyRateSnapshot,
                TotalWorkerWages = wCost,
                MachineHourlyRateSnapshot = mac.MachineHourlyRateSnapshot,
                TotalMachineOperatingCost = mCost
            });
        }

        _context.Moqaysat.Add(moqaysa);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم حفظ المقايسة بنجاح", moqaysaId = moqaysa.MoqaysaId });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MOQAYSA", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] MoqaysaSaveDto dto)
    {
        var moqaysa = await _context.Moqaysat
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id);

        if (moqaysa == null) return NotFound(new { message = "المقايسة غير موجودة" });

        if (moqaysa.IsIncludedInTasweya)
            return BadRequest(new { message = "لا يمكن تعديل المقايسة لأنها مُدرجة بالفعل في كشف تسوية / مطالبة رسمية" });

        var calc = await PerformCalculationAsync(dto);

        moqaysa.JobOrderNumber = dto.JobOrderNumber.Trim();
        moqaysa.CustomerId = dto.CustomerId;
        moqaysa.RequiredQuantityText = dto.RequiredQuantityText?.Trim() ?? "";
        moqaysa.SpecificationsText = dto.SpecificationsText?.Trim() ?? "";

        moqaysa.RawMaterials = calc.RawMaterials;
        moqaysa.MaterialProcessingFee = calc.MaterialProcessingFee;
        moqaysa.FilmsProcessingFee = calc.FilmsProcessingFee;
        moqaysa.TotalMaterials = calc.TotalMaterials;
        moqaysa.TotalWorkerWages = calc.TotalWorkerWages;
        moqaysa.TotalMachineCosts = calc.TotalMachineCosts;
        moqaysa.SubTotal1 = calc.SubTotal1;
        moqaysa.ApplyAdminExpenses = calc.ApplyAdminExpenses;
        moqaysa.AdminExpenses = calc.AdminExpenses;
        moqaysa.TotalExpenses = calc.TotalExpenses;
        moqaysa.ApplyMarkup = calc.ApplyMarkup;
        moqaysa.SelectedMarkupPercent = calc.SelectedMarkupPercent;
        moqaysa.MarkupAmount = calc.MarkupAmount;
        moqaysa.TotalAfterMarkup = calc.TotalAfterMarkup;
        moqaysa.MinistryOfFinanceFee = calc.MinistryOfFinanceFee;
        moqaysa.WorkersSyndicateFund = calc.WorkersSyndicateFund;
        moqaysa.SubTotal2 = calc.SubTotal2;
        moqaysa.ApplyOther = calc.ApplyOther;
        moqaysa.OtherAmount = calc.OtherAmount;
        moqaysa.SubTotal3 = calc.SubTotal3;
        moqaysa.ApplyVat = calc.ApplyVat;
        moqaysa.VatAmount = calc.VatAmount;
        moqaysa.FinalGrandTotal = calc.FinalGrandTotal;

        // Replace Items
        _context.MoqaysaMaterialItems.RemoveRange(moqaysa.MaterialItems);
        _context.MoqaysaMachineryItems.RemoveRange(moqaysa.MachineryItems);

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
            decimal wCost = MoqaysaCalculationService.Round2(mac.Hours * mac.WorkerHourlyRateSnapshot);
            decimal mCost = MoqaysaCalculationService.Round2(mac.Hours * mac.MachineHourlyRateSnapshot);
            moqaysa.MachineryItems.Add(new MoqaysaMachineryItem
            {
                MachineryId = mac.MachineryId,
                MachineryNameSnapshot = mac.MachineryNameSnapshot?.Trim() ?? "آلة",
                Hours = mac.Hours,
                WorkerHourlyRateSnapshot = mac.WorkerHourlyRateSnapshot,
                TotalWorkerWages = wCost,
                MachineHourlyRateSnapshot = mac.MachineHourlyRateSnapshot,
                TotalMachineOperatingCost = mCost
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تحديث المقايسة بنجاح" });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("MOQAYSA", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.Moqaysat.FindAsync(id);
        if (item == null) return NotFound(new { message = "المقايسة غير موجودة" });

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = item.IsActive ? "تم تفعيل المقايسة" : "تم إلغاء تفعيل المقايسة", isActive = item.IsActive });
    }

    [HttpGet("{id}/export-word")]
    public async Task<IActionResult> ExportWord(int id)
    {
        var item = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id);

        if (item == null) return NotFound();

        byte[] bytes = _wordExportService.ExportMoqaysaToWord(item);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Moqaysa_{item.JobOrderNumber}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var item = await _context.Moqaysat
            .Include(m => m.Customer)
            .Include(m => m.MaterialItems)
            .Include(m => m.MachineryItems)
            .FirstOrDefaultAsync(m => m.MoqaysaId == id);

        if (item == null) return NotFound();

        byte[] bytes = _pdfExportService.ExportMoqaysaToPdf(item);
        return File(bytes, "application/pdf", $"Moqaysa_{item.JobOrderNumber}.pdf");
    }

    private async Task<MoqaysaCalculationResult> PerformCalculationAsync(MoqaysaSaveDto dto)
    {
        // Calculate Raw Materials sum from items
        decimal rawMaterialsSum = dto.MaterialItems.Sum(m => MoqaysaCalculationService.Round2(m.Quantity * m.UnitPrice));

        // Calculate Worker wages and machine operating costs from machinery items
        decimal totalWorkerWages = dto.MachineryItems.Sum(m => MoqaysaCalculationService.Round2(m.Hours * m.WorkerHourlyRateSnapshot));
        decimal totalMachineCosts = dto.MachineryItems.Sum(m => MoqaysaCalculationService.Round2(m.Hours * m.MachineHourlyRateSnapshot));

        // Load formula configs
        var configs = await _context.CostingFormulaConfigs.Where(c => c.IsActive).ToListAsync();
        var matProcCfg = configs.FirstOrDefault(c => c.FieldKey == "MATERIAL_PROCESSING");
        var adminExpCfg = configs.FirstOrDefault(c => c.FieldKey == "ADMIN_EXPENSES");
        var mofCfg = configs.FirstOrDefault(c => c.FieldKey == "MINISTRY_OF_FINANCE");
        var workersCfg = configs.FirstOrDefault(c => c.FieldKey == "WORKERS_FUND");
        var otherCfg = configs.FirstOrDefault(c => c.FieldKey == "OTHER");
        var vatCfg = configs.FirstOrDefault(c => c.FieldKey == "VAT");

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

            MaterialProcessingPercent = matProcCfg?.DefaultPercentage ?? 5.5m,
            AdminExpensesPercent = adminExpCfg?.DefaultPercentage ?? 8.5m,
            MinistryOfFinancePercent = mofCfg?.DefaultPercentage ?? 15.0m,
            WorkersFundPercent = workersCfg?.DefaultPercentage ?? 10.0m,
            WorkersFundBaseMode = workersCfg?.WorkersFundBaseMode ?? "DiffTotalMinusMaterials",
            OtherPercent = otherCfg?.DefaultPercentage ?? 10.0m,
            VatPercent = vatCfg?.DefaultPercentage ?? 14.0m
        };

        return _calculationService.Calculate(input);
    }
}
