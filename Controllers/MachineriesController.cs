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
public class MachineriesController : ControllerBase
{
    private readonly IMachineryService _machineryService;

    public MachineriesController(IMachineryService machineryService)
    {
        _machineryService = machineryService;
    }

    [HttpGet]
    [PermissionAuthorize("MACHINERY", "CanAccess")]
    public async Task<ActionResult<List<MachineryViewModel>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _machineryService.GetAllAsync(activeOnly, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("MACHINERY", "CanAccess")]
    public async Task<ActionResult<MachineryViewModel>> GetById(int id, CancellationToken ct)
    {
        var item = await _machineryService.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound(new { message = "الآلة غير موجودة" });

        return Ok(item);
    }

    [HttpPost]
    [PermissionAuthorize("MACHINERY", "CanCreate")]
    public async Task<IActionResult> Create(
        [FromBody] MachineryDto dto,
        CancellationToken ct)
    {
        try
        {
            var item = await _machineryService.CreateAsync(dto, ct);
            return Ok(new
            {
                message = "تمت إضافة الآلة بنجاح",
                machineryId = item.MachineryId,
                item
            });
        }
        catch (System.ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MACHINERY", "CanUpdate")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] MachineryDto dto,
        CancellationToken ct)
    {
        try
        {
            var item = await _machineryService.UpdateAsync(id, dto, ct);
            if (item == null)
                return NotFound(new { message = "الآلة غير موجودة" });

            return Ok(new
            {
                message = "تم تعديل بيانات الآلة وأجور الساعات بنجاح",
                item
            });
        }
        catch (System.ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/maintenance-status")]
    [PermissionAuthorize("MACHINERY", "CanUpdate")]
    public async Task<IActionResult> UpdateMaintenanceStatus(
        int id,
        [FromBody] UpdateMachineryMaintenanceDto dto,
        CancellationToken ct)
    {
        try
        {
            var item = await _machineryService.UpdateMaintenanceStatusAsync(id, dto, ct);
            if (item == null)
                return NotFound(new { message = "الآلة غير موجودة" });

            return Ok(new
            {
                message = "تم تحديث حالة صيانة الآلة بنجاح",
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
    [PermissionAuthorize("MACHINERY", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var exists = await _machineryService.GetByIdAsync(id, ct);
        if (exists == null)
            return NotFound(new { message = "الآلة غير موجودة" });

        var isActive = await _machineryService.ToggleActiveAsync(id, ct);
        return Ok(new
        {
            message = isActive ? "تم تفعيل الآلة" : "تم تعطيل الآلة",
            isActive
        });
    }
}
