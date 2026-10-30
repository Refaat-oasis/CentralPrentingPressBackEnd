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
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public record CustomerDto(
        string CustomerNameAr,
        int AccountId,
        string? Phone,
        string? Email,
        string? Address,
        string? Notes
    );

    public record AccountSaveDto(
        string AccountNameAr,
        string? AccountCode,
        string? DebitHeaderTextAr
    );

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts([FromQuery] bool includeInactive = false)
    {
        var query = _context.Accounts.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        var accounts = await query
            .OrderBy(a => a.AccountId)
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpPost("accounts")]
    [PermissionAuthorize("CUSTOMERS", "CanCreate")]
    public async Task<IActionResult> CreateAccount([FromBody] AccountSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccountNameAr))
            return BadRequest(new { message = "يرجى إدخال اسم الحساب المحاسبي" });

        int nextId = (await _context.Accounts.MaxAsync(a => (int?)a.AccountId) ?? 0) + 1;

        var account = new AccountEntity
        {
            AccountId = nextId,
            AccountCode = string.IsNullOrWhiteSpace(dto.AccountCode) ? $"ACC-{nextId}" : dto.AccountCode.Trim(),
            AccountNameAr = dto.AccountNameAr.Trim(),
            DebitHeaderTextAr = string.IsNullOrWhiteSpace(dto.DebitHeaderTextAr)
                ? $"من حـ / {dto.AccountNameAr.Trim()}"
                : dto.DebitHeaderTextAr.Trim(),
            IsActive = true
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تمت إضافة الحساب المحاسبي بنجاح", accountId = account.AccountId });
    }

    [HttpPut("accounts/{id}")]
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] AccountSaveDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null) return NotFound(new { message = "الحساب المحاسبي غير موجود" });

        if (string.IsNullOrWhiteSpace(dto.AccountNameAr))
            return BadRequest(new { message = "يرجى إدخال اسم الحساب المحاسبي" });

        account.AccountNameAr = dto.AccountNameAr.Trim();
        if (!string.IsNullOrWhiteSpace(dto.AccountCode)) account.AccountCode = dto.AccountCode.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DebitHeaderTextAr)) account.DebitHeaderTextAr = dto.DebitHeaderTextAr.Trim();

        await _context.SaveChangesAsync();

        return Ok(new { message = "تم تعديل بيانات الحساب المحاسبي بنجاح" });
    }

    [HttpPatch("accounts/{id}/toggle-active")]
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> ToggleAccountActive(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null) return NotFound(new { message = "الحساب المحاسبي غير موجود" });

        account.IsActive = !account.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = account.IsActive ? "تم تفعيل الحساب المحاسبي بنجاح" : "تم تعطيل الحساب المحاسبي بنجاح",
            isActive = account.IsActive
        });
    }

    [HttpGet]
    [PermissionAuthorize("CUSTOMERS", "CanAccess")]
    public async Task<IActionResult> GetAll([FromQuery] int? accountId, [FromQuery] bool activeOnly = false)
    {
        var query = _context.Customers
            .Include(c => c.Account)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(c => c.AccountId == accountId.Value);

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        var list = await query
            .OrderBy(c => c.CustomerNameAr)
            .Select(c => new
            {
                c.CustomerId,
                c.CustomerNameAr,
                c.AccountId,
                AccountNameAr = c.Account != null ? c.Account.AccountNameAr : "غير محدد",
                DebitHeaderTextAr = c.Account != null ? c.Account.DebitHeaderTextAr : "",
                c.Phone,
                c.Email,
                c.Address,
                c.Notes,
                c.IsActive
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    [PermissionAuthorize("CUSTOMERS", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerNameAr))
            return BadRequest(new { message = "يرجى كتابة اسم الجهة الطالبة / العميل" });

        var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == dto.AccountId);
        if (!accountExists)
            return BadRequest(new { message = "الحساب المختار غير صحيح" });

        var customer = new Customer
        {
            CustomerNameAr = dto.CustomerNameAr.Trim(),
            AccountId = dto.AccountId,
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تمت إضافة الجهة الطالبة بنجاح", customerId = customer.CustomerId });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("CUSTOMERS", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "الجهة الطالبة غير موجودة" });

        var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == dto.AccountId);
        if (!accountExists)
            return BadRequest(new { message = "الحساب المختار غير صحيح" });

        customer.CustomerNameAr = dto.CustomerNameAr.Trim();
        customer.AccountId = dto.AccountId;
        customer.Phone = dto.Phone?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تعديل بيانات الجهة الطالبة بنجاح" });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("CUSTOMERS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "الجهة الطالبة غير موجودة" });

        customer.IsActive = !customer.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = customer.IsActive ? "تم تفعيل الجهة الطالبة" : "تم تعطيل الجهة الطالبة", isActive = customer.IsActive });
    }
}
