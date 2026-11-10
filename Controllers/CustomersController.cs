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
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<List<AccountViewModel>>> GetAccounts(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var accounts = await _customerService.GetAllAccountsAsync(!includeInactive, ct);
        return Ok(accounts);
    }

    [HttpPost("accounts")]
    [PermissionAuthorize("CUSTOMERS", "CanCreate")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] AccountSaveDto dto,
        CancellationToken ct)
    {
        try
        {
            var account = await _customerService.CreateAccountAsync(dto, ct);
            return Ok(new
            {
                message = "تمت إضافة الحساب المحاسبي بنجاح",
                accountId = account.AccountId,
                account
            });
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

    [HttpPut("accounts/{id}")]
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> UpdateAccount(
        int id,
        [FromBody] AccountSaveDto dto,
        CancellationToken ct)
    {
        try
        {
            var account = await _customerService.UpdateAccountAsync(id, dto, ct);
            if (account == null)
                return NotFound(new { message = "الحساب المحاسبي غير موجود" });

            return Ok(new
            {
                message = "تم تعديل بيانات الحساب المحاسبي بنجاح",
                account
            });
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

    [HttpPatch("accounts/{id}/toggle-active")]
    [HttpPut("accounts/{id}/toggle-active")]
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> ToggleAccountActive(int id, CancellationToken ct)
    {
        var exists = await _customerService.GetAccountByIdAsync(id, ct);
        if (exists == null)
            return NotFound(new { message = "الحساب المحاسبي غير موجود" });

        var isActive = await _customerService.ToggleAccountActiveAsync(id, ct);
        return Ok(new
        {
            message = isActive ? "تم تفعيل الحساب المحاسبي بنجاح" : "تم تعطيل الحساب المحاسبي بنجاح",
            isActive
        });
    }

    [HttpGet]
    [PermissionAuthorize("CUSTOMERS", "CanAccess")]
    public async Task<ActionResult<List<CustomerViewModel>>> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _customerService.GetAllCustomersAsync(activeOnly, ct);
        if (accountId.HasValue)
        {
            list = list.FindAll(c => c.AccountId == accountId.Value);
        }
        return Ok(list);
    }

    [HttpPost]
    [PermissionAuthorize("CUSTOMERS", "CanCreate")]
    public async Task<IActionResult> Create(
        [FromBody] CustomerDto dto,
        CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(dto, ct);
            return Ok(new
            {
                message = "تمت إضافة الجهة الطالبة بنجاح",
                customerId = customer.CustomerId,
                customer
            });
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
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CustomerDto dto,
        CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, dto, ct);
            if (customer == null)
                return NotFound(new { message = "الجهة الطالبة غير موجودة" });

            return Ok(new
            {
                message = "تم تعديل بيانات الجهة الطالبة بنجاح",
                customer
            });
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
    [PermissionAuthorize("CUSTOMERS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var exists = await _customerService.GetCustomerByIdAsync(id, ct);
        if (exists == null)
            return NotFound(new { message = "الجهة الطالبة غير موجودة" });

        var isActive = await _customerService.ToggleCustomerActiveAsync(id, ct);
        return Ok(new
        {
            message = isActive ? "تم تفعيل الجهة الطالبة" : "تم تعطيل الجهة الطالبة",
            isActive
        });
    }
}
