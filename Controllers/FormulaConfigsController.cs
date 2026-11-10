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
[Route("api/formula-configs")]
[Authorize]
public class FormulaConfigsController : ControllerBase
{
    private readonly IFormulaConfigService _formulaConfigService;

    public FormulaConfigsController(IFormulaConfigService formulaConfigService)
    {
        _formulaConfigService = formulaConfigService;
    }

    [HttpGet]
    [PermissionAuthorize("FORMULAS", "CanAccess")]
    public async Task<ActionResult<List<FormulaConfigViewModel>>> GetAll(CancellationToken ct)
    {
        var list = await _formulaConfigService.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpPut]
    [PermissionAuthorize("FORMULAS", "CanUpdate")]
    public async Task<IActionResult> UpdateConfigs(
        [FromBody] List<UpdateFormulaConfigDto> items,
        CancellationToken ct)
    {
        try
        {
            await _formulaConfigService.UpdateConfigsAsync(items, ct);
            return Ok(new { message = "تم حفظ إعدادات نسب ومعادلات المقايسة بنجاح" });
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
}
