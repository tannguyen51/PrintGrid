using MediatR;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.Enums;
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
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IConfiguration configuration,
    Commands.PlaceOrder.OrderDeliveryService delivery) : IRequestHandler<ReviewQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(ReviewQuoteCommand command, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetByIdAsync(command.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<QuoteDto>(Error.NotFound("Quote", command.QuoteId));
        if (quote.Status != QuoteStatus.PendingReview)
            return Result.Failure<QuoteDto>(Error.Conflict("Chỉ duyệt báo giá đã được khách gửi. Bản nháp tính giá chưa vào hàng đợi."));

        var pendingOrder = await orders.GetByQuoteIdAsync(quote.Id, cancellationToken);
        if (pendingOrder is null || pendingOrder.Status != OrderStatus.AwaitingApproval)
            return Result.Failure<QuoteDto>(Error.Conflict("Không tìm thấy đơn đang chờ duyệt của báo giá này."));

        var band = decimal.TryParse(configuration["QuoteReview:AdjustmentBandPercent"], out var configuredBand)
            ? configuredBand : 10m;
        var validityHours = double.TryParse(configuration["QuoteReview:ValidityHours"], out var configuredHours)
            ? configuredHours : 48d;

        var result = quote.Approve(command.StaffId, command.TotalPrice, command.PromisedDeliveryDate,
            command.Reason, clock.UtcNow, TimeSpan.FromHours(validityHours), false, band);
        if (result.IsFailure)
            return Result.Failure<QuoteDto>(result.Error);

        DateOnly? promisedDeliveryDate = null;
        if (pendingOrder.RequestedDeliveryDate is { } requested)
        {
            var assessment = await delivery.AssessAsync(quote, requested, cancellationToken);
            if (assessment.IsFailure) return Result.Failure<QuoteDto>(assessment.Error);
            if (!assessment.Value.CanMeetRequestedDate)
                return Result.Failure<QuoteDto>(Error.Conflict($"Chưa thể duyệt: ngày giao sớm nhất là {assessment.Value.EarliestDeliveryDate:dd/MM/yyyy}, sau ngày khách mong muốn."));
            promisedDeliveryDate = assessment.Value.PromisedDeliveryDate;
        }
        var activate = pendingOrder.ApplyApprovedQuote(quote, promisedDeliveryDate);
        if (activate.IsFailure) return Result.Failure<QuoteDto>(activate.Error);
        var conversion = quote.MarkConverted(clock.UtcNow);
        if (conversion.IsFailure) return Result.Failure<QuoteDto>(conversion.Error);

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
