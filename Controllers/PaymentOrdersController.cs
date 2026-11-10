using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Filters;
using CentralPress.API.Models;
using CentralPress.API.Services;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/payment-orders")]
[Route("api/paymentorders")]
[Authorize]
public class PaymentOrdersController : ControllerBase
{
    private readonly IPaymentOrderService _paymentOrderService;

    public PaymentOrdersController(IPaymentOrderService paymentOrderService)
    {
        _paymentOrderService = paymentOrderService;
    }

    [HttpGet("unpaid-tasweyat")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<ActionResult<List<UnpaidTasweyaViewModel>>> GetUnpaidTasweyat(
        [FromQuery] int? accountId,
        [FromQuery] int? customerId,
        CancellationToken ct)
    {
        var list = await _paymentOrderService.GetUnpaidTasweyatAsync(accountId, customerId, ct);
        return Ok(list);
    }

    [HttpGet("budget-validation")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<ActionResult<BudgetValidationResultViewModel>> ValidateBudget(
        [FromQuery] int accountId,
        [FromQuery] decimal amount,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _paymentOrderService.ValidateBudgetAsync(accountId, amount, customerId, ct);
        return Ok(result);
    }

    [HttpGet]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<ActionResult<List<PaymentOrderSummaryViewModel>>> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] int? customerId,
        [FromQuery] PaymentOrderStatus? status,
        [FromQuery] bool? isConfirmed,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var list = await _paymentOrderService.GetAllAsync(accountId, customerId, status, isConfirmed, activeOnly, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<ActionResult<PaymentOrderDetailViewModel>> GetById(int id, CancellationToken ct)
    {
        var item = await _paymentOrderService.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound(new { message = "أمر الدفع غير موجود" });

        return Ok(item);
    }

    [HttpPost]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentOrderDto dto, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.CreateAsync(dto, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message, remainingBudget = result.RemainingBudget });

        return Ok(new
        {
            message = result.Message,
            paymentOrderId = result.PaymentOrderId,
            isConfirmed = result.Status == PaymentOrderStatus.Approved || result.Status == PaymentOrderStatus.Paid,
            status = result.Status.ToString(),
            totalPaidAmount = result.TotalPaidAmount
        });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentOrderDto dto, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.UpdateAsync(id, dto, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message, remainingBudget = result.RemainingBudget });

        return Ok(new { message = result.Message, paymentOrderId = result.PaymentOrderId });
    }

    [HttpPost("{id}/confirm")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> ConfirmPaymentOrder(int id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.ConfirmAsync(id, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            message = result.Message,
            isConfirmed = true,
            status = result.Status.ToString()
        });
    }

    [HttpPost("{id}/status")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePaymentOrderStatusDto dto, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.UpdateStatusAsync(id, dto, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message, remainingBudget = result.RemainingBudget });

        return Ok(new
        {
            message = result.Message,
            paymentOrderId = result.PaymentOrderId,
            status = result.Status.ToString(),
            totalPaidAmount = result.TotalPaidAmount,
            remainingBudget = result.RemainingBudget
        });
    }

    [HttpPost("{id}/submit-review")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> SubmitForReview(int id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.SubmitForReviewAsync(id, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("{id}/approve")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.ApproveAsync(id, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message, remainingBudget = result.RemainingBudget });

        return Ok(result);
    }

    [HttpPost("{id}/execute-payment")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> ExecutePayment(int id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.ExecutePaymentAsync(id, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> Reject(int id, [FromBody] UpdatePaymentOrderStatusDto dto, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.RejectAsync(id, dto, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanUpdate")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPaymentOrderDto dto, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _paymentOrderService.CancelAsync(id, dto, currentUserId, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (success, message, isActive) = await _paymentOrderService.ToggleActiveAsync(id, currentUserId, ct);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, isActive });
    }

    [HttpGet("{id}/export-word")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<IActionResult> ExportWord(int id, CancellationToken ct)
    {
        byte[]? bytes = await _paymentOrderService.ExportWordAsync(id, ct);
        if (bytes == null) return NotFound();

        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"PaymentOrder_{id}.docx");
    }

    [HttpGet("{id}/export-pdf")]
    [PermissionAuthorize("PAYMENT_ORDERS", "CanAccess")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken ct)
    {
        byte[]? bytes = await _paymentOrderService.ExportPdfAsync(id, ct);
        if (bytes == null) return NotFound();

        return File(bytes, "application/pdf", $"PaymentOrder_{id}.pdf");
    }
}
