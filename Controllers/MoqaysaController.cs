using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoqaysaController : ControllerBase
{
    private readonly IMoqaysaService _moqaysaService;
    private readonly IPaperCuttingCalculationService _cuttingService;

    public MoqaysaController(
        IMoqaysaService moqaysaService,
        IPaperCuttingCalculationService cuttingService)
    {
        _moqaysaService = moqaysaService;
        _cuttingService = cuttingService;
    }

    [HttpPost("calculate-preview")]
    public async Task<ActionResult<MoqaysaCalculationResultViewModel>> CalculatePreview(
        [FromBody] MoqaysaSaveDto dto,
        CancellationToken ct)
    {
        var result = await _moqaysaService.CalculatePreviewAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("calculate-paper-cutting")]
    public ActionResult<PaperCuttingCalculationResultViewModel> CalculatePaperCutting(
        [FromBody] PaperCuttingCalculationRequestDto request)
    {
        var result = _cuttingService.CalculateCuttingYield(request);
        return Ok(result);
    }

    [HttpGet]
    [PermissionAuthorize("MOQAYSA", "CanAccess")]
    public async Task<ActionResult<List<MoqaysaSummaryViewModel>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] string? jobOrderNumber,
        [FromQuery] bool? isIncludedInTasweya,
        [FromQuery] bool? isPaid,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _moqaysaService.GetAllAsync(customerId, jobOrderNumber, isIncludedInTasweya, isPaid, activeOnly, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("MOQAYSA", "CanAccess")]
    public async Task<ActionResult<MoqaysaDetailViewModel>> GetById(int id, CancellationToken ct)
    {
        var detail = await _moqaysaService.GetByIdAsync(id, ct);
        if (detail == null)
            return NotFound(new { message = "المقايسة غير موجودة" });

        return Ok(detail);
    }

    [HttpPost]
    [PermissionAuthorize("MOQAYSA", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] MoqaysaSaveDto dto, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            var created = await _moqaysaService.CreateAsync(dto, userId, ct);
            return Ok(new { message = "تم حفظ المقايسة بنجاح", moqaysaId = created.MoqaysaId, item = created });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MOQAYSA", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] MoqaysaSaveDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await _moqaysaService.UpdateAsync(id, dto, ct);
            if (updated == null)
                return NotFound(new { message = "المقايسة غير موجودة" });

            return Ok(new { message = "تم تحديث المقايسة بنجاح", item = updated });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/toggle-active")]
    [HttpPut("{id}/toggle-active")]
    [PermissionAuthorize("MOQAYSA", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var exists = await _moqaysaService.GetByIdAsync(id, ct);
        if (exists == null)
            return NotFound(new { message = "المقايسة غير موجودة" });

        var isActive = await _moqaysaService.ToggleActiveAsync(id, ct);
        return Ok(new
        {
            message = isActive ? "تم تفعيل المقايسة" : "تم إلغاء تفعيل المقايسة",
            isActive
        });
    }

    [HttpGet("{id}/export-word")]
    public async Task<IActionResult> ExportWord(int id, CancellationToken ct)
    {
        var bytes = await _moqaysaService.ExportWordAsync(id, ct);
        if (bytes == null) return NotFound();

        var detail = await _moqaysaService.GetByIdAsync(id, ct);
        var jobOrder = detail?.JobOrderNumber ?? id.ToString();
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Moqaysa_{jobOrder}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken ct)
    {
        var bytes = await _moqaysaService.ExportPdfAsync(id, ct);
        if (bytes == null) return NotFound();

        var detail = await _moqaysaService.GetByIdAsync(id, ct);
        var jobOrder = detail?.JobOrderNumber ?? id.ToString();
        return File(bytes, "application/pdf", $"Moqaysa_{jobOrder}.pdf");
    }
}
