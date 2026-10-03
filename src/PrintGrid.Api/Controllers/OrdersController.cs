using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Policy = Policies.RequireCustomer)]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetOrdersQuery(customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetOrderTimeline(Guid id, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await _sender.Send(new GetOrderTimelineQuery(id, customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var isEmailVerified = User.HasClaim(c => c.Type == "email_verified" && c.Value == "true");
        if (!isEmailVerified)
        {
            return StatusCode(403, new { error = new { code = "EMAIL_NOT_VERIFIED", message = "Please verify your email to place an order." } });
        }

        var result = await _sender.Send(
            new PlaceOrderCommand(
                customerId.Value,
                request.QuoteId,
                request.Street,
                request.Ward,
                request.District,
                request.City,
                request.PostalCode,
                request.AcceptTerms),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });

        return CreatedAtAction(nameof(PlaceOrder), new { id = result.Value.Id }, result.Value);
    }
}

public record PlaceOrderRequest(
    Guid QuoteId,
    string Street,
    string Ward,
    string District,
    string City,
    string PostalCode,
    bool AcceptTerms);
