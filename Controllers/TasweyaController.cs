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
public class TasweyaController : ControllerBase
{
    private readonly ITasweyaService _tasweyaService;

    public TasweyaController(ITasweyaService tasweyaService)
    {
        _tasweyaService = tasweyaService;
    }

    [HttpGet("unsettled-moqaysat")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<ActionResult<List<TasweyaUnsettledMoqaysaViewModel>>> GetUnsettledMoqaysat(
        [FromQuery] int? customerId,
        [FromQuery] int? accountId,
        [FromQuery] int? includeTasweyaId,
        CancellationToken ct)
    {
        var list = await _tasweyaService.GetUnsettledMoqaysatAsync(customerId, accountId, includeTasweyaId, ct);
        return Ok(list);
    }

    [HttpGet]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<ActionResult<List<TasweyaSummaryViewModel>>> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] int? customerId,
        [FromQuery] bool? isPaid,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _tasweyaService.GetAllAsync(accountId, customerId, isPaid, activeOnly, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<ActionResult<TasweyaDetailViewModel>> GetById(int id, CancellationToken ct)
    {
        var item = await _tasweyaService.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound(new { message = "كشف التسوية غير موجود" });

        return Ok(item);
    }

    [HttpPost]
    [PermissionAuthorize("TASWEYA", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] CreateTasweyaDto dto, CancellationToken ct)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (success, message, result) = await _tasweyaService.CreateAsync(dto, currentUserId, ct);

            if (!success || result == null)
                return BadRequest(new { message });

            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = $"تعذر إصدار كشف التسوية: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("TASWEYA", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTasweyaDto dto, CancellationToken ct)
    {
        try
        {
            var (success, message, tasweyaId) = await _tasweyaService.UpdateAsync(id, dto, ct);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, tasweyaId });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = $"تعذر تحديث كشف التسوية: {ex.Message}" });
        }
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("TASWEYA", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var (success, message, isActive) = await _tasweyaService.ToggleActiveAsync(id, ct);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, isActive });
    }

    [HttpGet("{id}/export-word")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<IActionResult> ExportWord(int id, CancellationToken ct)
    {
        byte[]? bytes = await _tasweyaService.ExportWordAsync(id, ct);
        if (bytes == null) return NotFound();

        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Tasweya_{id}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    [PermissionAuthorize("TASWEYA", "CanAccess")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken ct)
    {
        byte[]? bytes = await _tasweyaService.ExportPdfAsync(id, ct);
        if (bytes == null) return NotFound();

        return File(bytes, "application/pdf", $"Tasweya_{id}.pdf");
    }
}
