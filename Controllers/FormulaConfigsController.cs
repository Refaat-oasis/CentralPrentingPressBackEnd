using System.Collections.Generic;
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
public class FormulaConfigsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FormulaConfigsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public record UpdateFormulaConfigDto(
        int FormulaConfigId,
        decimal DefaultPercentage,
        string? WorkersFundBaseMode,
        string? AllowedOptionsCsv
    );

    [HttpGet]
    [PermissionAuthorize("FORMULAS", "CanAccess")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.CostingFormulaConfigs
            .OrderBy(c => c.StepOrder)
            .ToListAsync();

        return Ok(list);
    }

    [HttpPut]
    [PermissionAuthorize("FORMULAS", "CanUpdate")]
    public async Task<IActionResult> UpdateConfigs([FromBody] List<UpdateFormulaConfigDto> items)
    {
        foreach (var item in items)
        {
            var config = await _context.CostingFormulaConfigs.FindAsync(item.FormulaConfigId);
            if (config != null)
            {
                config.DefaultPercentage = item.DefaultPercentage;
                if (!string.IsNullOrWhiteSpace(item.WorkersFundBaseMode))
                {
                    config.WorkersFundBaseMode = item.WorkersFundBaseMode;
                }
                if (item.AllowedOptionsCsv != null)
                {
                    config.AllowedOptionsCsv = item.AllowedOptionsCsv.Trim();
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حفظ إعدادات نسب ومعادلات المقايسة بنجاح" });
    }
}
