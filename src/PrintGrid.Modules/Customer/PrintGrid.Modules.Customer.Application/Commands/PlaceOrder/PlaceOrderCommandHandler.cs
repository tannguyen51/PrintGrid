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
    IConfiguration configuration,
    OrderDeliveryService delivery)
    : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var validation = await new PlaceOrderCommandValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<OrderDto>(Error.Validation(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        var quote = await quotes.GetByIdAsync(command.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<OrderDto>(Error.NotFound("Quote", command.QuoteId));
        if (quote.CustomerId != command.CustomerId)
            return Result.Failure<OrderDto>(Error.Forbidden("Quote belongs to another customer"));
        if (command.TechConstraintsJson is not null || command.PinnedModelId.HasValue || command.ReferenceLineId.HasValue
            || command.ModelBindingKind != "không")
            return Result.Failure<OrderDto>(Error.Validation("Yêu cầu kỹ thuật và model máy phải được lưu trong báo giá. Hãy tạo báo giá mới để thay đổi cấu hình."));

        var requiresOpsApproval = quote.Status == QuoteStatus.Draft;
        if (quote.Status != QuoteStatus.Ready && !requiresOpsApproval)
            return Result.Failure<OrderDto>(Error.Conflict("Báo giá chưa sẵn sàng để đặt hàng."));
        if (await orders.GetByQuoteIdAsync(quote.Id, cancellationToken) is not null)
            return Result.Failure<OrderDto>(Error.Conflict("Báo giá này đã được dùng để đặt hàng."));

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

        var assessment = await delivery.AssessAsync(quote, command.RequestedDeliveryDate!.Value, cancellationToken);
        if (assessment.IsFailure) return Result.Failure<OrderDto>(assessment.Error);
        if (!assessment.Value.CanMeetRequestedDate)
            return Result.Failure<OrderDto>(Error.Conflict($"Ngày giao sớm nhất hiện tại là {assessment.Value.EarliestDeliveryDate:dd/MM/yyyy}. Vui lòng chọn lại ngày mong muốn."));

        if (requiresOpsApproval)
        {
            var submission = quote.SubmitForReview();
            if (submission.IsFailure) return Result.Failure<OrderDto>(submission.Error);
        }
        else
        {
            var conversion = quote.MarkConverted(clock.UtcNow);
            if (conversion.IsFailure)
                return Result.Failure<OrderDto>(conversion.Error);
        }

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
            referenceLineId: command.ReferenceLineId,
            requestedDeliveryDate: command.RequestedDeliveryDate,
            promisedDeliveryDate: assessment.Value.PromisedDeliveryDate);

        // Placement never manufactures a payment, including for the automatic dispatch lane.
        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(order));
    }

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
            i.ReferenceOrderLineId)).ToList(),
        order.RequestedDeliveryDate,
        order.DispatchMode);
}
