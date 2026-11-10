using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IMachineryService
{
    Task<List<MachineryViewModel>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<MachineryViewModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MachineryViewModel> CreateAsync(MachineryDto dto, CancellationToken ct = default);
    Task<MachineryViewModel?> UpdateAsync(int id, MachineryDto dto, CancellationToken ct = default);
    Task<MachineryViewModel?> UpdateMaintenanceStatusAsync(int id, UpdateMachineryMaintenanceDto dto, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);
}
