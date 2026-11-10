using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class CustomerService : ICustomerService
{
    private static readonly SemaphoreSlim _accountLock = new(1, 1);
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerViewModel>> GetAllCustomersAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.Customers
            .Include(c => c.Account)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        var list = await query
            .OrderBy(c => c.CustomerNameAr)
            .ToListAsync(ct);

        return list.Select(MapToCustomerViewModel).ToList();
    }

    public async Task<CustomerViewModel?> GetCustomerByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.CustomerId == id, ct);

        return customer == null ? null : MapToCustomerViewModel(customer);
    }

    public async Task<CustomerViewModel> CreateCustomerAsync(CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.CustomerNameAr))
            throw new ArgumentException("يرجى كتابة اسم الجهة الطالبة / العميل", nameof(dto.CustomerNameAr));

        var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == dto.AccountId, ct);
        if (!accountExists)
            throw new InvalidOperationException("الحساب المختار غير صحيح أو غير موجود");

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
        await _context.SaveChangesAsync(ct);

        await _context.Entry(customer).Reference(c => c.Account).LoadAsync(ct);

        return MapToCustomerViewModel(customer);
    }

    public async Task<CustomerViewModel?> UpdateCustomerAsync(int id, CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer == null) return null;

        if (string.IsNullOrWhiteSpace(dto.CustomerNameAr))
            throw new ArgumentException("يرجى كتابة اسم الجهة الطالبة / العميل", nameof(dto.CustomerNameAr));

        var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == dto.AccountId, ct);
        if (!accountExists)
            throw new InvalidOperationException("الحساب المختار غير صحيح أو غير موجود");

        customer.CustomerNameAr = dto.CustomerNameAr.Trim();
        customer.AccountId = dto.AccountId;
        customer.Phone = dto.Phone?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync(ct);
        await _context.Entry(customer).Reference(c => c.Account).LoadAsync(ct);

        return MapToCustomerViewModel(customer);
    }

    public async Task<bool> ToggleCustomerActiveAsync(int id, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer == null) return false;

        customer.IsActive = !customer.IsActive;
        await _context.SaveChangesAsync(ct);
        return customer.IsActive;
    }

    public async Task<List<AccountViewModel>> GetAllAccountsAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.Accounts.AsQueryable();
        if (activeOnly)
            query = query.Where(a => a.IsActive);

        var list = await query
            .OrderBy(a => a.AccountId)
            .ToListAsync(ct);

        return list.Select(MapToAccountViewModel).ToList();
    }

    public async Task<AccountViewModel?> GetAccountByIdAsync(int id, CancellationToken ct = default)
    {
        var account = await _context.Accounts.FindAsync(new object[] { id }, ct);
        return account == null ? null : MapToAccountViewModel(account);
    }

    public async Task<AccountViewModel> CreateAccountAsync(AccountSaveDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.AccountNameAr))
            throw new ArgumentException("يرجى إدخال اسم الحساب المحاسبي", nameof(dto.AccountNameAr));

        await _accountLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(dto.AccountCode))
            {
                var trimmedCode = dto.AccountCode.Trim();
                var codeExists = await _context.Accounts
                    .IgnoreQueryFilters()
                    .AnyAsync(a => a.AccountCode == trimmedCode, ct);
                if (codeExists)
                    throw new InvalidOperationException($"كود الحساب المحاسبي '{trimmedCode}' مستخدم بالفعل");
            }

            if (_context.Database.IsRelational())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                    var account = await GenerateAndSaveAccountInternalAsync(dto, ct);

                    await transaction.CommitAsync(ct);
                    return account;
                });
            }
            else
            {
                return await GenerateAndSaveAccountInternalAsync(dto, ct);
            }
        }
        finally
        {
            _accountLock.Release();
        }
    }

    private async Task<AccountViewModel> GenerateAndSaveAccountInternalAsync(AccountSaveDto dto, CancellationToken ct)
    {
        int currentMaxId = await _context.Accounts
            .IgnoreQueryFilters()
            .MaxAsync(a => (int?)a.AccountId, ct) ?? 0;

        int nextId = currentMaxId + 1;
        string accountCode = string.IsNullOrWhiteSpace(dto.AccountCode)
            ? $"ACC-{nextId}"
            : dto.AccountCode.Trim();

        while (await _context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.AccountCode == accountCode, ct))
        {
            nextId++;
            accountCode = $"ACC-{nextId}";
        }

        var account = new AccountEntity
        {
            AccountId = nextId,
            AccountCode = accountCode,
            AccountNameAr = dto.AccountNameAr.Trim(),
            DebitHeaderTextAr = FormatDebitHeaderText(dto.DebitHeaderTextAr, dto.AccountNameAr),
            IsActive = true
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(ct);

        return MapToAccountViewModel(account);
    }

    public async Task<AccountViewModel?> UpdateAccountAsync(int id, AccountSaveDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var account = await _context.Accounts.FindAsync(new object[] { id }, ct);
        if (account == null) return null;

        if (string.IsNullOrWhiteSpace(dto.AccountNameAr))
            throw new ArgumentException("يرجى إدخال اسم الحساب المحاسبي", nameof(dto.AccountNameAr));

        if (!string.IsNullOrWhiteSpace(dto.AccountCode))
        {
            var trimmedCode = dto.AccountCode.Trim();
            var codeExists = await _context.Accounts
                .IgnoreQueryFilters()
                .AnyAsync(a => a.AccountCode == trimmedCode && a.AccountId != id, ct);
            if (codeExists)
                throw new InvalidOperationException($"كود الحساب المحاسبي '{trimmedCode}' مستخدم بالفعل");
            account.AccountCode = trimmedCode;
        }

        account.AccountNameAr = dto.AccountNameAr.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DebitHeaderTextAr))
            account.DebitHeaderTextAr = FormatDebitHeaderText(dto.DebitHeaderTextAr, dto.AccountNameAr);

        await _context.SaveChangesAsync(ct);
        return MapToAccountViewModel(account);
    }

    private static string FormatDebitHeaderText(string? text, string fallbackName)
    {
        var raw = string.IsNullOrWhiteSpace(text) ? fallbackName : text.Trim();
        var cleaned = System.Text.RegularExpressions.Regex.Replace(raw, @"^من\s*حـ?\s*[\/\\:]\s*", "").Trim();
        return $"من حـ / {cleaned}";
    }

    public async Task<bool> ToggleAccountActiveAsync(int id, CancellationToken ct = default)
    {
        var account = await _context.Accounts.FindAsync(new object[] { id }, ct);
        if (account == null) return false;

        account.IsActive = !account.IsActive;
        await _context.SaveChangesAsync(ct);
        return account.IsActive;
    }

    private static CustomerViewModel MapToCustomerViewModel(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        CustomerNameAr = c.CustomerNameAr,
        AccountId = c.AccountId,
        AccountCode = c.Account?.AccountCode,
        AccountNameAr = c.Account?.AccountNameAr ?? "غير محدد",
        DebitHeaderTextAr = c.Account?.DebitHeaderTextAr ?? "",
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        Notes = c.Notes,
        IsActive = c.IsActive
    };

    private static AccountViewModel MapToAccountViewModel(AccountEntity a) => new()
    {
        AccountId = a.AccountId,
        AccountCode = a.AccountCode,
        AccountNameAr = a.AccountNameAr,
        DebitHeaderTextAr = a.DebitHeaderTextAr,
        IsActive = a.IsActive
    };
}
