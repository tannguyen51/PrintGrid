using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Payments;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-CUST-012 / BR-PAY-007/008 — deposit and settlement. Placement no longer confirms an order;
/// ops records payments here and the order is confirmed only once the deposit is covered.
/// The customer reads their own payment snapshot; ops/admin record payments.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrderPaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public OrderPaymentsController(ISender sender) => _sender = sender;

    /// <summary>Ops queue: orders still waiting for their deposit (PaymentPending).</summary>
    [HttpGet("pending-payment")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetPendingPayment(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrdersPendingPaymentQuery(search), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpGet("{orderId:guid}/payment")]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> GetPayment(Guid orderId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetOrderPaymentQuery(orderId, customerId.Value), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/mark-paid")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> MarkPaid(
        Guid orderId,
        [FromBody] MarkOrderPaidRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkOrderPaidCommand(orderId, request?.Amount), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    private IActionResult ToError(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = error.Code, message = error.Message } });
    }
}

/// <summary>Omit <paramref name="Amount"/> to settle the full remaining balance.</summary>
public sealed record MarkOrderPaidRequest(decimal? Amount = null);
