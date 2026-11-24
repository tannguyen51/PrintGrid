using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Policy = Policies.RequireCustomer)]
public class OrdersController(ISender sender,
    PrintGrid.Modules.Customer.Domain.Repositories.IQuoteRepository quotes,
    OrderDeliveryService delivery) : ControllerBase
{
    [HttpPost("delivery-preview")]
    public async Task<IActionResult> PreviewDelivery(DeliveryPreviewRequest request, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(request.QuoteId, ct);
        if (quote is null) return NotFound();
        if (quote.CustomerId != User.GetCustomerId()) return Forbid();
        if (quote.Status is not (PrintGrid.Modules.Customer.Domain.Enums.QuoteStatus.Ready or PrintGrid.Modules.Customer.Domain.Enums.QuoteStatus.Draft)
            || quote.ExpiresAt <= DateTime.UtcNow)
            return Conflict(new { error = new { code = "conflict", message = "Báo giá chưa được duyệt hoặc đã hết hạn." } });
        var result = await delivery.AssessAsync(quote, request.RequestedDeliveryDate, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new GetOrdersQuery(customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetOrderTimeline(Guid id, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new GetOrderTimelineQuery(id, customerId.Value), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid id, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new ConfirmOrderReceiptCommand(id, customerId.Value), cancellationToken);
        if (!result.IsFailure) return NoContent();
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }

    /// <summary>
    /// Customer confirms or rejects COD (Cash on Delivery) receipt.
    /// Called when customer receives notification that order is ready for delivery.
    /// Customer has 24h to confirm (BR-PAY-008 COD model).
    /// </summary>
    [HttpPost("{id:guid}/cod-confirm")]
    public async Task<IActionResult> ConfirmCodReceipt(
        Guid id,
        [FromBody] ConfirmCodReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new ConfirmCodReceiptCommand(id, request.Accepted, customerId.Value), cancellationToken);
        if (!result.IsFailure) return Ok(new { message = "Xác nhận COD thành công." });
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }

    /// <summary>
    /// Hub/ops requests COD confirmation from customer.
    /// Called automatically when order enters QualityCheck status, or manually by ops.
    /// Sets 24h deadline for customer to respond.
    /// </summary>
    [HttpPost("{id:guid}/request-cod-confirmation")]
    [Authorize(Policy = Policies.RequireHub)]
    public async Task<IActionResult> RequestCodConfirmation(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RequestCodConfirmationCommand(id), cancellationToken);
        if (!result.IsFailure) return Ok(new { message = "Đã yêu cầu khách hàng xác nhận nhận hàng COD." });
        var status = result.Error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var isEmailVerified = User.HasClaim(c => c is { Type: "email_verified", Value: "true" });
        if (!isEmailVerified)
        {
            return StatusCode(403, new { error = new { code = "EMAIL_NOT_VERIFIED", message = "Please verify your email to place an order." } });
        }

        var result = await sender.Send(
            new PlaceOrderCommand(
                customerId.Value,
                request.QuoteId,
                request.Street,
                request.Ward,
                request.District,
                request.City,
                request.PostalCode,
                request.AcceptTerms,
                request.TechConstraintsJson,
                request.ModelBindingKind,
                request.PinnedModelId,
                request.ReferenceLineId,
                request.RequestedDeliveryDate),
            cancellationToken);

        if (!result.IsFailure) return CreatedAtAction(nameof(PlaceOrder), new { id = result.Value.Id }, result.Value);
        var status = result.Error.Code == "conflict"
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });

    }

    [HttpGet("{id:guid}/reprint-requests")]
    public async Task<IActionResult> GetReprintRequests(Guid id, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new GetReprintRequestsQuery(id, customerId.Value), cancellationToken);
        return result.IsFailure ? NotFound(new { error = result.Error }) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/reprint-request")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> CreateReprintRequest(Guid id, [FromBody] ReprintRequestBody request, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var result = await sender.Send(new CreateReprintRequestCommand(
            id, customerId.Value, request.Reason, request.Description, request.Photos), cancellationToken);
        if (!result.IsFailure) return CreatedAtAction(nameof(GetReprintRequests), new { id }, result.Value);
        var status = result.Error.Code == "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;
        return StatusCode(status, new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}

public record PlaceOrderRequest(
    Guid QuoteId,
    string Street,
    string Ward,
    string District,
    string City,
    string PostalCode,
    bool AcceptTerms,
    // Doc §6.1 / §6.2 — machine selection context (BR-03/04/05/09)
    string? TechConstraintsJson = null,
    string ModelBindingKind = "không",
    Guid? PinnedModelId = null,
    Guid? ReferenceLineId = null,
    DateOnly? RequestedDeliveryDate = null);

public record DeliveryPreviewRequest(Guid QuoteId, DateOnly RequestedDeliveryDate);

public record ReprintRequestBody(string Reason, string Description, IReadOnlyList<string> Photos);

/// <summary>Request body for COD confirmation endpoint.</summary>
public record ConfirmCodReceiptRequest(bool Accepted);
