using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IMoqaysaService
{
    Task<MoqaysaCalculationResultViewModel> CalculatePreviewAsync(MoqaysaSaveDto dto, CancellationToken ct = default);
    Task<List<MoqaysaSummaryViewModel>> GetAllAsync(
        int? customerId = null,
        string? jobOrderNumber = null,
        bool? isIncludedInTasweya = null,
        bool? isPaid = null,
        bool activeOnly = false,
        CancellationToken ct = default);
    Task<MoqaysaDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MoqaysaDetailViewModel> CreateAsync(MoqaysaSaveDto dto, string? userId, CancellationToken ct = default);
    Task<MoqaysaDetailViewModel?> UpdateAsync(int id, MoqaysaSaveDto dto, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default);
    Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default);
}
