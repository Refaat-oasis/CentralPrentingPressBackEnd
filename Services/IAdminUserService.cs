using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IAdminUserService
{
    Task<List<EmployeeSummaryViewModel>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDetailViewModel?> GetEmployeeByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, EmployeeDetailViewModel? Employee)> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> UpdateEmployeeAsync(string id, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, bool IsActive)> ToggleActiveAsync(string id, string currentUserId, CancellationToken cancellationToken = default);
    Task<List<UserPermissionOverrideViewModel>> GetUserOverridesAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> SaveUserOverridesAsync(string userId, SaveOverridesDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> DeleteEmployeeAsync(string id, string currentUserId, CancellationToken cancellationToken = default);
}
