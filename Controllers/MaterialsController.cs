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
public class MaterialsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MaterialsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public record MaterialDto(string MaterialNameAr, string UnitNameAr);

    [HttpGet]
    [PermissionAuthorize("MATERIALS", "CanAccess")]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _context.Materials.AsQueryable();
        if (activeOnly)
            query = query.Where(m => m.IsActive);

        var list = await query
            .OrderBy(m => m.MaterialNameAr)
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    [PermissionAuthorize("MATERIALS", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] MaterialDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MaterialNameAr))
            return BadRequest(new { message = "يرجى كتابة اسم الخامة" });

        var item = new Material
        {
            MaterialNameAr = dto.MaterialNameAr.Trim(),
            UnitNameAr = string.IsNullOrWhiteSpace(dto.UnitNameAr) ? "فرخ" : dto.UnitNameAr.Trim(),
            IsActive = true
        };

        _context.Materials.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تمت إضافة الخامة بنجاح", item });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("MATERIALS", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] MaterialDto dto)
    {
        var item = await _context.Materials.FindAsync(id);
        if (item == null) return NotFound(new { message = "الخامة غير موجودة" });

        item.MaterialNameAr = dto.MaterialNameAr.Trim();
        item.UnitNameAr = string.IsNullOrWhiteSpace(dto.UnitNameAr) ? "فرخ" : dto.UnitNameAr.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تعديل الخامة بنجاح", item });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("MATERIALS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.Materials.FindAsync(id);
        if (item == null) return NotFound(new { message = "الخامة غير موجودة" });

        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = item.IsActive ? "تم تفعيل الخامة" : "تم تعطيل الخامة", isActive = item.IsActive });
    }
}
