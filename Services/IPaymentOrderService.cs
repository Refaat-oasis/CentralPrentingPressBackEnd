using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Models;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IPaymentOrderService
{
    Task<List<UnpaidTasweyaViewModel>> GetUnpaidTasweyatAsync(
        int? accountId,
        int? customerId,
        CancellationToken ct = default);

    Task<List<PaymentOrderSummaryViewModel>> GetAllAsync(
        int? accountId = null,
        int? customerId = null,
        PaymentOrderStatus? status = null,
        bool? isConfirmed = null,
        bool activeOnly = false,
        CancellationToken ct = default);

    Task<PaymentOrderDetailViewModel?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> CreateAsync(
        CreatePaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> UpdateAsync(
        int id,
        UpdatePaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> SubmitForReviewAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> ApproveAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> ExecutePaymentAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> RejectAsync(
        int id,
        UpdatePaymentOrderStatusDto dto,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> CancelAsync(
        int id,
        CancelPaymentOrderDto dto,
        string? currentUserId,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> ConfirmAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default);

    Task<BudgetValidationResultViewModel> ValidateBudgetAsync(
        int accountId,
        decimal amount,
        int? customerId = null,
        CancellationToken ct = default);

    Task<PaymentOrderOperationResultViewModel> UpdateStatusAsync(
        int id,
        UpdatePaymentOrderStatusDto dto,
        string? currentUserId,
        CancellationToken ct = default);

    Task<(bool Success, string Message, bool? IsActive)> ToggleActiveAsync(
        int id,
        string? currentUserId,
        CancellationToken ct = default);

    Task<byte[]?> ExportWordAsync(int id, CancellationToken ct = default);

    Task<byte[]?> ExportPdfAsync(int id, CancellationToken ct = default);
}
