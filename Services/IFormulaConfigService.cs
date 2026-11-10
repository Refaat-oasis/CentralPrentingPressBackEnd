using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IFormulaConfigService
{
    Task<List<FormulaConfigViewModel>> GetAllAsync(CancellationToken ct = default);
    Task<FormulaConfigViewModel?> GetByFieldKeyAsync(string fieldKey, CancellationToken ct = default);
    Task UpdateConfigsAsync(List<UpdateFormulaConfigDto> items, CancellationToken ct = default);
}
