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
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("accounts-summary")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<ActionResult<List<AccountSummaryViewModel>>> GetAccountsSummary(CancellationToken ct = default)
    {
        var summary = await _reportsService.GetAccountsSummaryAsync(ct);
        return Ok(summary);
    }

    [HttpGet("customers-summary")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<ActionResult<List<CustomerSummaryViewModel>>> GetCustomersSummary(
        [FromQuery] CustomerSummaryFilterDto filter,
        CancellationToken ct = default)
    {
        var summary = await _reportsService.GetCustomersSummaryAsync(filter, ct);
        return Ok(summary);
    }

    [HttpGet("moqaysat-detailed")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<ActionResult<MoqaysatDetailedReportViewModel>> GetMoqaysatDetailed(
        [FromQuery] MoqaysatDetailedFilterDto filter,
        CancellationToken ct = default)
    {
        var report = await _reportsService.GetMoqaysatDetailedAsync(filter, ct);
        return Ok(report);
    }

    [HttpGet("payment-orders-register")]
    [PermissionAuthorize("REPORTS", "CanAccess")]
    public async Task<ActionResult<List<PaymentOrderRegisterItemViewModel>>> GetPaymentOrdersRegister(
        [FromQuery] PaymentOrdersRegisterFilterDto filter,
        CancellationToken ct = default)
    {
        var register = await _reportsService.GetPaymentOrdersRegisterAsync(filter, ct);
        return Ok(register);
    }
}
