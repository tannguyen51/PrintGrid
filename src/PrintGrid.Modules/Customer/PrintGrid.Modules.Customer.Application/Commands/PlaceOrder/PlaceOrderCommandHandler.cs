using System.Globalization;
using MediatR;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public class PlaceOrderCommandHandler(
    IQuoteRepository quotes,
    IOrderRepository orders,
    IModelRepository models,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IConfiguration configuration)
    : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetByIdAsync(command.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<OrderDto>(Error.NotFound("Quote", command.QuoteId));
        if (quote.CustomerId != command.CustomerId)
            return Result.Failure<OrderDto>(Error.Forbidden("Quote belongs to another customer"));

        // Moderation gate: a model can be rejected after the quote was published, so the
        // approval is re-checked at conversion time, before the quote is consumed.
        foreach (var modelId in quote.Items.Select(i => i.ModelId).Distinct())
        {
            var model = await models.GetByIdAsync(modelId, cancellationToken);
            if (model is null || model.ReviewStatus != ModelReviewStatus.Approved)
            {
                return Result.Failure<OrderDto>(Error.Conflict(
                    "Một model trong báo giá chưa được duyệt nội dung hoặc đã bị từ chối."));
            }
        }

        var conversion = quote.MarkConverted(clock.UtcNow);
        if (conversion.IsFailure)
            return Result.Failure<OrderDto>(conversion.Error);

        var address = Address.Create(
            command.Street, command.Ward, command.District, command.City, command.PostalCode);

        var sequence = await orders.CountAsync(cancellationToken) + 1;
        var orderNumber = $"PG-{clock.Today:yyyyMMdd}-{sequence:D5}";

        var order = Order.CreateFromQuote(
            quote, address, orderNumber, DepositPercent(),
            termsAccepted: command.AcceptTerms, termsAcceptedAtUtc: clock.UtcNow,
            techConstraintsJson: command.TechConstraintsJson,
            modelBindingKind: command.ModelBindingKind,
            pinnedModelId: command.PinnedModelId,
            referenceLineId: command.ReferenceLineId);

        // Demo flow: record the full payment up front. ApplyPayment also raises
        // OrderConfirmedEvent (jobs are created immediately) and sets PaidAmount, so the
        // hub's "ship" guard (IsSettled) passes later. With the flag off, the order stays
        // PaymentPending until ops records the deposit via POST /orders/{id}/mark-paid.
        if (AutoConfirmOnPlace())
        {
            var paid = order.ApplyPayment(order.TotalPrice.Amount, $"AUTO-{Guid.NewGuid():N}");
            if (paid.IsFailure)
                return Result.Failure<OrderDto>(paid.Error);
        }

        // When auto-confirm is disabled the order stays PaymentPending and no
        // OrderConfirmedEvent is raised until the deposit is recorded.
        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(order));
    }

    /// <summary>Defaults to true (demo): placement confirms the order without a separate payment step.</summary>
    private bool AutoConfirmOnPlace()
        => !bool.TryParse(configuration["Payment:AutoConfirmOnPlace"], out var enabled) || enabled;

    /// <summary>Reads "Payment:DepositPercent" (50 means 50%) and falls back to 50 when absent/invalid.</summary>
    private decimal DepositPercent()
    {
        var raw = configuration["Payment:DepositPercent"];
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var percent)
            ? Math.Clamp(percent, 0m, 100m)
            : 50m;
    }

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        order.TotalPrice.Amount,
        order.TotalPrice.Currency,
        order.PromisedDeliveryDate,
        order.CreatedAt,
        order.DepositAmount,
        order.RefundedAmount,
        order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ModelId,
            i.Quantity,
            i.Configuration.MaterialCode,
            i.Configuration.ColorCode,
            i.Configuration.LayerHeightMm,
            i.Configuration.InfillPercent,
            i.UnitPrice.Amount,
            i.TechConstraintsJson,
            i.ModelBindingKind,
            i.PinnedModelId,
            i.BindingLevel,
            i.ReferenceOrderLineId)).ToList());
}
