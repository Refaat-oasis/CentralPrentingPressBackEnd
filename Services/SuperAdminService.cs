using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly SuperAdminTotpValidator _totpValidator;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;

    public SuperAdminService(
        SuperAdminTotpValidator totpValidator,
        ITokenService tokenService,
        ApplicationDbContext context)
    {
        _totpValidator = totpValidator;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<SuperAdminLoginResponseViewModel?> LoginAsync(
        SuperAdminLoginDto dto,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return null;

        bool isValid = _totpValidator.ValidateAndConsumeCode(dto.Code);
        if (!isValid)
        {
            return null;
        }

        var superAdminVirtualUser = new ApplicationUser
        {
            Id = "SUPERADMIN_ROOT",
            Email = "superadmin@aup.edu.eg",
            EmployeeCode = "SUPERADMIN",
            FullName = "المسؤول الفائق (Super Admin)",
            JobTitle = new JobTitle { TitleNameAr = "Super Administrator" }
        };

        var tokens = await _tokenService.GenerateTokensAsync(superAdminVirtualUser, isSuperAdmin: true, ipAddress);

        return new SuperAdminLoginResponseViewModel
        {
            Token = tokens.AccessToken,
            User = new UserProfileViewModel
            {
                Id = "SUPERADMIN_ROOT",
                EmployeeCode = "SUPERADMIN",
                FullName = "المسؤول الفائق (Super Admin)",
                Email = "superadmin@aup.edu.eg",
                JobTitleName = "Super Administrator",
                MustChangePassword = false,
                IsSuperAdmin = true
            }
        };
    }

    public async Task<PagedResultViewModel<AuditBacklogViewModel>> GetAuditBacklogAsync(
        AuditBacklogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        query ??= new AuditBacklogQueryDto();

        var queryable = _context.AuditBacklogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.ModuleName))
        {
            queryable = queryable.Where(b => b.ModuleName == query.ModuleName);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            queryable = queryable.Where(b =>
                (b.EmployeeName != null && b.EmployeeName.Contains(search)) ||
                (b.ActionType != null && b.ActionType.Contains(search)) ||
                (b.RequestPath != null && b.RequestPath.Contains(search)) ||
                (b.NewValuesJson != null && b.NewValuesJson.Contains(search))
            );
        }

        int totalCount = await queryable.CountAsync(cancellationToken);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int page = Math.Max(1, query.Page);

        var items = await queryable
            .OrderByDescending(b => b.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AuditBacklogViewModel
            {
                AuditId = b.AuditId,
                UserId = b.UserId,
                EmployeeName = b.EmployeeName,
                JobTitleName = b.JobTitleName,
                ActionType = b.ActionType,
                ModuleName = b.ModuleName,
                EntityName = b.EntityName,
                RecordId = b.RecordId,
                RequestPath = b.RequestPath,
                OldValuesJson = b.OldValuesJson,
                NewValuesJson = b.NewValuesJson,
                IpAddress = b.IpAddress,
                TimestampUtc = b.TimestampUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResultViewModel<AuditBacklogViewModel>(items, totalCount, page, pageSize);
    }
}
