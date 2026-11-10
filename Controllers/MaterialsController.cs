using System.Collections.Generic;
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
public class MaterialsController : ControllerBase
{
    private readonly IMaterialService _materialService;

    public MaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    [HttpGet]
    [PermissionAuthorize("MATERIALS", "CanAccess")]
    public async Task<ActionResult<List<MaterialViewModel>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _materialService.GetAllAsync(activeOnly, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("MATERIALS", "CanAccess")]
    public async Task<ActionResult<MaterialViewModel>> GetById(int id, CancellationToken ct)
    {
        var item = await _materialService.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound(new { message = "الخامة غير موجودة" });

        return Ok(item);
    }

    [HttpPost]
    [PermissionAuthorize("MATERIALS", "CanCreate")]
    public async Task<IActionResult> Create(
        [FromBody] MaterialDto dto,
        CancellationToken ct)
    {
        try
        {
            var item = await _materialService.CreateAsync(dto, ct);
            return Ok(new
            {
                message = "تمت إضافة الخامة بنجاح",
                materialId = item.MaterialId,
                item
            });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MATERIALS", "CanUpdate")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] MaterialDto dto,
        CancellationToken ct)
    {
        try
        {
            var item = await _materialService.UpdateAsync(id, dto, ct);
            if (item == null)
                return NotFound(new { message = "الخامة غير موجودة" });

            return Ok(new
            {
                message = "تم تعديل الخامة بنجاح",
                item
            });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/toggle-active")]
    [HttpPut("{id}/toggle-active")]
    [PermissionAuthorize("MATERIALS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var exists = await _materialService.GetByIdAsync(id, ct);
        if (exists == null)
            return NotFound(new { message = "الخامة غير موجودة" });

        var isActive = await _materialService.ToggleActiveAsync(id, ct);
        return Ok(new
        {
            message = isActive ? "تم تفعيل الخامة" : "تم تعطيل الخامة",
            isActive
        });
    }
}
