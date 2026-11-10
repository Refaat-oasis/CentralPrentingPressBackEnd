using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface ITasweyaService
{
    Task<List<TasweyaUnsettledMoqaysaViewModel>> GetUnsettledMoqaysatAsync(
        int? customerId,
        int? accountId,
        int? includeTasweyaId = null,
        CancellationToken ct = default);

    Task<List<TasweyaSummaryViewModel>> GetAllAsync(
        int? accountId,
        int? customerId,
        bool? isPaid,
        bool activeOnly = false,
        CancellationToken ct = default);

    Task<TasweyaDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(bool Success, string Message, TasweyaCreateResultViewModel? Result)> CreateAsync(
        CreateTasweyaDto dto,
        string? userId,
        CancellationToken ct = default);

    Task<(bool Success, string Message, int? TasweyaId)> UpdateAsync(
        int id,
        UpdateTasweyaDto dto,
        CancellationToken ct = default);

    Task<(bool Success, string Message, bool? IsActive)> ToggleActiveAsync(
        int id,
        CancellationToken ct = default);

    Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default);

    Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default);
}
