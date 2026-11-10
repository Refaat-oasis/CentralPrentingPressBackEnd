using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IMaterialService
{
    Task<List<MaterialViewModel>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<MaterialViewModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MaterialViewModel> CreateAsync(MaterialDto dto, CancellationToken ct = default);
    Task<MaterialViewModel?> UpdateAsync(int id, MaterialDto dto, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);
}
