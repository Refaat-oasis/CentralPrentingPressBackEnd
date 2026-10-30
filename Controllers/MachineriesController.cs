using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MachineriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MachineriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public record MachineryDto(
        string MachineryNameAr,
        decimal WorkerHourlyFee,
        decimal MachineOperatingHourlyFee,
        int DisplayOrder
    );

    [HttpGet]
    [PermissionAuthorize("MACHINERY", "CanAccess")]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _context.Machineries.AsQueryable();
        if (activeOnly)
            query = query.Where(m => m.IsActive);

        var list = await query
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.MachineryId)
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    [PermissionAuthorize("MACHINERY", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] MachineryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MachineryNameAr))
            return BadRequest(new { message = "يرجى كتابة اسم الآلة / العملية" });

        var machinery = new Machinery
        {
            MachineryNameAr = dto.MachineryNameAr.Trim(),
            WorkerHourlyFee = dto.WorkerHourlyFee,
            MachineOperatingHourlyFee = dto.MachineOperatingHourlyFee,
            DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : (_context.Machineries.Max(m => (int?)m.DisplayOrder) ?? 0) + 1,
            IsActive = true
        };

        _context.Machineries.Add(machinery);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تمت إضافة الآلة بنجاح", item = machinery });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MACHINERY", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] MachineryDto dto)
    {
        var item = await _context.Machineries.FindAsync(id);
        if (item == null) return NotFound(new { message = "الآلة غير موجودة" });

        item.MachineryNameAr = dto.MachineryNameAr.Trim();
        item.WorkerHourlyFee = dto.WorkerHourlyFee;
        item.MachineOperatingHourlyFee = dto.MachineOperatingHourlyFee;
        if (dto.DisplayOrder > 0) item.DisplayOrder = dto.DisplayOrder;

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تعديل بيانات الآلة وأجور الساعات بنجاح", item });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("MACHINERY", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.Machineries.FindAsync(id);
        if (item == null) return NotFound(new { message = "الآلة غير موجودة" });

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = item.IsActive ? "تم تفعيل الآلة" : "تم تعطيل الآلة", isActive = item.IsActive });
    }
}
