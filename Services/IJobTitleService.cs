using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IJobTitleService
{
    Task<List<JobTitleSummaryViewModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobTitleSummaryViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, int? TitleId)> CreateAsync(JobTitleDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> UpdateAsync(int id, JobTitleDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, bool IsActive)> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);
    Task<RolePermissionsMatrixViewModel?> GetRolePermissionsAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> SaveRolePermissionsAsync(int id, List<RolePermissionInputDto> permissions, CancellationToken cancellationToken = default);
    Task<List<ModulePermissionViewModel>> GetSystemModulesAsync(CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
