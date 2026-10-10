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
/// on <c>POST /orders/{orderId}/confirm-receipt</c>. Requires the hub policy.
/// </summary>
[ApiController]
[Route("api/v1/hub/orders")]
[Authorize(Policy = Policies.RequireHub)]
public class ShipmentController : ControllerBase
{
    private readonly ISender _sender;

    public ShipmentController(ISender sender) => _sender = sender;

    [HttpGet("shipment-queue")]
    public async Task<IActionResult> GetShipmentQueue(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetShipmentQueueQuery(), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/ship")]
    public async Task<IActionResult> Ship(Guid orderId, [FromBody] ShipRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ShipOrderCommand(orderId, request.TrackingNumber), cancellationToken);
        return ToResult(result);
    }

    [HttpPost("{orderId:guid}/deliver")]
    public async Task<IActionResult> Deliver(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeliverOrderCommand(orderId), cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(Result result)
    {
        if (result.IsSuccess) return NoContent();
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public record ShipRequest(string TrackingNumber);
