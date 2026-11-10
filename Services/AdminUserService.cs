using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CentralPress.API.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _tokenService = tokenService;
    }

    public async Task<List<EmployeeSummaryViewModel>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.JobTitle)
            .OrderBy(u => u.EmployeeCode)
            .Select(u => new EmployeeSummaryViewModel
            {
                Id = u.Id,
                EmployeeCode = u.EmployeeCode,
                FullName = u.FullName,
                Email = u.Email,
                PhotoUrl = u.PhotoUrl,
                JobTitleId = u.JobTitleId,
                JobTitleName = u.JobTitle != null ? u.JobTitle.TitleNameAr : "غير محدد",
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeDetailViewModel?> GetEmployeeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null) return null;

        return new EmployeeDetailViewModel
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            JobTitleId = user.JobTitleId,
            JobTitleName = user.JobTitle != null ? user.JobTitle.TitleNameAr : "غير محدد",
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public async Task<(bool Succeeded, string Message, EmployeeDetailViewModel? Employee)> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeCode) || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
            return (false, "يرجى ملء كافة الحقول الأساسية (الكود، الاسم، البريد)", null);

        if (await _context.Users.AnyAsync(u => u.EmployeeCode == dto.EmployeeCode, cancellationToken))
            return (false, "كود الموظف مستخدم بالفعل", null);

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email, cancellationToken))
            return (false, "البريد الإلكتروني مستخدم بالفعل", null);

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            EmployeeCode = dto.EmployeeCode.Trim(),
            FullName = dto.FullName.Trim(),
            JobTitleId = dto.JobTitleId,
            PhotoUrl = dto.PhotoUrl?.Trim(),
            MustChangePassword = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        string passwordToSet = !string.IsNullOrWhiteSpace(dto.DummyPassword)
            ? dto.DummyPassword.Trim()
            : "User@" + DateTime.UtcNow.Year;

        var result = await _userManager.CreateAsync(user, passwordToSet);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        // Assign default Employee role if no role assigned
        await _userManager.AddToRoleAsync(user, "Employee");

        // Send invitation email in background
        _ = _emailService.SendNewUserInvitationCodeAsync(user.Email, user.FullName, user.EmployeeCode, passwordToSet);

        var jobTitle = user.JobTitleId.HasValue ? await _context.JobTitles.FindAsync([user.JobTitleId.Value], cancellationToken) : null;

        var vm = new EmployeeDetailViewModel
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            JobTitleId = user.JobTitleId,
            JobTitleName = jobTitle?.TitleNameAr ?? "غير محدد",
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return (true, "تمت إضافة الموظف بنجاح", vm);
    }

    public async Task<(bool Succeeded, string Message)> UpdateEmployeeAsync(string id, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "الموظف غير موجود");

        if (string.IsNullOrWhiteSpace(dto.EmployeeCode) || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
            return (false, "يرجى ملء كافة الحقول الأساسية");

        if (await _context.Users.AnyAsync(u => u.EmployeeCode == dto.EmployeeCode && u.Id != id, cancellationToken))
            return (false, "كود الموظف مستخدم لدى حساب آخر");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id, cancellationToken))
            return (false, "البريد الإلكتروني مستخدم لدى حساب آخر");

        user.EmployeeCode = dto.EmployeeCode.Trim();
        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        user.JobTitleId = dto.JobTitleId;
        user.PhotoUrl = dto.PhotoUrl?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (true, "تم تحديث بيانات الموظف بنجاح");
    }

    public async Task<(bool Succeeded, string Message, bool IsActive)> ToggleActiveAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
    {
        // MED-05: Prevent self-deactivation
        if (id == currentUserId)
        {
            return (false, "لا يمكن للمستخدم تعطيل حسابه الشخصي", false);
        }

        // Use IgnoreQueryFilters so we can find deactivated accounts
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return (false, "الموظف غير موجود", false);

        // MED-05: Protect SuperAdmin accounts from deactivation
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            return (false, "لا يمكن تعطيل حساب المسؤول الفائق", user.IsActive);
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        // If deactivated, revoke all active refresh tokens immediately
        if (!user.IsActive)
        {
            await _tokenService.RevokeAllUserTokensAsync(user.Id, null);
        }

        string msg = user.IsActive ? "تم تفعيل حساب الموظف بنجاح" : "تم تعطيل حساب الموظف بنجاح";
        return (true, msg, user.IsActive);
    }

    public async Task<List<UserPermissionOverrideViewModel>> GetUserOverridesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPermissionOverrides
            .Where(o => o.UserId == userId)
            .Select(o => new UserPermissionOverrideViewModel
            {
                OverrideId = o.OverrideId,
                UserId = o.UserId,
                ModuleId = o.ModuleId,
                CanAccess = o.CanAccess,
                CanCreate = o.CanCreate,
                CanUpdate = o.CanUpdate,
                CanToggleActive = o.CanToggleActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string Message)> SaveUserOverridesAsync(string userId, SaveOverridesDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "الموظف غير موجود");

        // HIGH-22: Wrap multi-step mutations in database transaction with provider safety check
        await using IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var existing = await _context.UserPermissionOverrides
                .Where(o => o.UserId == userId)
                .ToListAsync(cancellationToken);

            _context.UserPermissionOverrides.RemoveRange(existing);

            if (dto.Overrides != null && dto.Overrides.Any())
            {
                var newOverrides = dto.Overrides
                    .Where(o => o.CanAccess.HasValue || o.CanCreate.HasValue || o.CanUpdate.HasValue || o.CanToggleActive.HasValue)
                    .Select(o => new UserPermissionOverride
                    {
                        UserId = userId,
                        ModuleId = o.ModuleId,
                        CanAccess = o.CanAccess,
                        CanCreate = o.CanCreate,
                        CanUpdate = o.CanUpdate,
                        CanToggleActive = o.CanToggleActive
                    });

                await _context.UserPermissionOverrides.AddRangeAsync(newOverrides, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (true, "تم حفظ الصلاحيات الاستثنائية بنجاح");
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return (false, $"حدث خطأ أثناء حفظ الصلاحيات الاستثنائية: {ex.Message}");
        }
    }

    public async Task<(bool Succeeded, string Message)> DeleteEmployeeAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            return (false, "لا يمكن حذف حسابك الشخصي");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return (false, "الموظف غير موجود");

        // HIGH-05: Dynamic role check instead of hardcoded email/EMP001 check
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            return (false, "لا يمكن حذف حساب المسؤول الفائق للنظام");
        }

        // HIGH-22: Transactional cleanup with provider safety check
        await using IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            // Remove overrides and refresh tokens
            var overrides = await _context.UserPermissionOverrides.Where(o => o.UserId == id).ToListAsync(cancellationToken);
            _context.UserPermissionOverrides.RemoveRange(overrides);

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == id).ToListAsync(cancellationToken);
            _context.RefreshTokens.RemoveRange(tokens);

            await _context.SaveChangesAsync(cancellationToken);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (true, "تم حذف حساب الموظف بنجاح");
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return (false, $"حدث خطأ أثناء حذف الموظف: {ex.Message}");
        }
    }
}
