using MediatR;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public record ReviewQuoteCommand(
    Guid QuoteId,
    Guid StaffId,
    decimal TotalPrice,
    DateOnly PromisedDeliveryDate,
    string? Reason) : IRequest<Result<QuoteDto>>;

public sealed class ReviewQuoteCommandHandler(
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IConfiguration configuration) : IRequestHandler<ReviewQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(ReviewQuoteCommand command, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetByIdAsync(command.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Quote", command.QuoteId));

        var band = decimal.TryParse(configuration["QuoteReview:AdjustmentBandPercent"], out var configuredBand)
            ? configuredBand : 10m;
        var validityHours = double.TryParse(configuration["QuoteReview:ValidityHours"], out var configuredHours)
            ? configuredHours : 48d;

        var result = quote.Approve(command.StaffId, command.TotalPrice, command.PromisedDeliveryDate,
            command.Reason, clock.UtcNow, TimeSpan.FromHours(validityHours), false, band);
        if (result.IsFailure)
            return Result.Failure<QuoteDto>(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(QuoteMappings.ToDto(quote));
    }
}

public record GetQuoteDraftsQuery : IRequest<Result<IReadOnlyList<QuoteDto>>>;

public sealed class GetQuoteDraftsQueryHandler(IQuoteRepository quotes)
    : IRequestHandler<GetQuoteDraftsQuery, Result<IReadOnlyList<QuoteDto>>>
{
    public async Task<Result<IReadOnlyList<QuoteDto>>> Handle(GetQuoteDraftsQuery request, CancellationToken cancellationToken)
    {
        var drafts = await quotes.GetDraftsAsync(cancellationToken);
        return Result.Success<IReadOnlyList<QuoteDto>>(drafts.Select(QuoteMappings.ToDto).ToList());
    }
}
