using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/quote-reviews")]
[Authorize(Policy = Policies.RequireQuoteReview)]
public class QuoteReviewsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuoteDraftsQuery(), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost("{quoteId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid quoteId, ReviewQuoteRequest request, CancellationToken cancellationToken)
    {
        var staffId = User.GetCustomerId();
        if (staffId is null) return Forbid();

        var result = await sender.Send(new ReviewQuoteCommand(
            quoteId, staffId.Value, request.TotalPrice, request.PromisedDeliveryDate, request.Reason), cancellationToken);
        return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(result.Value);
    }
}

public record ReviewQuoteRequest(decimal TotalPrice, DateOnly PromisedDeliveryDate, string? Reason);
