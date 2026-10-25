using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// Hub goods-out / delivery (FR-HUB tail of the core flow). The hub ships the parcel (tracking required)
/// and later confirms physical handover. The customer's warranty anchor is NOT set here — that happens
/// on <c>POST /orders/{orderId}/confirm-receipt</c>. Hub actions require the hub policy; the refund
/// action is an ops/finance concern and requires the ops policy.
/// </summary>
[ApiController]
[Route("api/v1/hub/orders")]
public class ShipmentController : ControllerBase
{
    private readonly ISender _sender;

    public ShipmentController(ISender sender) => _sender = sender;

    [HttpGet("shipment-queue")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> GetShipmentQueue(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetShipmentQueueQuery(), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/ship")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> Ship(Guid orderId, [FromBody] ShipRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ShipOrderCommand(orderId, request.TrackingNumber), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("ship-consolidated")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> ShipConsolidated([FromBody] ShipConsolidatedRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ShipConsolidatedOrdersCommand(request.OrderIds, request.TrackingNumber),
            cancellationToken);

        if (result.IsSuccess) return Ok(result.Value);
        return ToError(result.Error);
    }

    [HttpPost("{orderId:guid}/deliver")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> Deliver(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeliverOrderCommand(orderId), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("deliver-consolidated")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> DeliverConsolidated([FromBody] DeliverConsolidatedRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeliverConsolidatedOrdersCommand(request.OrderIds), cancellationToken);
        return ToResult(result);
    }

    // Ops/finance — deliberately NOT the hub policy (method-level [Authorize] does not override the
    // controller's: policies combine, so the controller-level hub policy had to move onto the other actions).
    [HttpPost("{orderId:guid}/refund")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> RefundOrder(Guid orderId, [FromBody] RefundOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefundOrderWithinScopeCommand(orderId, request.Amount, request.Reason), cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(Result result)
    {
        if (result.IsSuccess) return NoContent();
        return ToError(result.Error);
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

public record ShipRequest(string TrackingNumber);

public record ShipConsolidatedRequest(IReadOnlyList<Guid> OrderIds, string TrackingNumber);

public record DeliverConsolidatedRequest(IReadOnlyList<Guid> OrderIds);

public record RefundOrderRequest(decimal Amount, string? Reason = null);
