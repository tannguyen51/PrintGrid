using System.Globalization;
using MediatR;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    private readonly IQuoteRepository _quotes;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly IConfiguration _configuration;

    public PlaceOrderCommandHandler(
        IQuoteRepository quotes,
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IConfiguration configuration)
    {
        _quotes = quotes;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _configuration = configuration;
    }

    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByIdAsync(command.QuoteId, cancellationToken);
        if (quote is null)
            return Result.Failure<OrderDto>(Error.NotFound("Quote", command.QuoteId));
        if (quote.CustomerId != command.CustomerId)
            return Result.Failure<OrderDto>(Error.Forbidden("Quote belongs to another customer"));

        var conversion = quote.MarkConverted(_clock.UtcNow);
        if (conversion.IsFailure)
            return Result.Failure<OrderDto>(conversion.Error);

        var address = Address.Create(
            command.Street, command.Ward, command.District, command.City, command.PostalCode);

        var sequence = await _orders.CountAsync(cancellationToken) + 1;
        var orderNumber = $"PG-{_clock.Today:yyyyMMdd}-{sequence:D5}";

        var order = Order.CreateFromQuote(quote, address, orderNumber, DepositPercent());

        // FR-CUST-012 / BR-PAY-007: placement only records the required deposit. The order stays
        // PaymentPending and no OrderConfirmedEvent is raised (so no jobs are scheduled) until
        // the deposit is actually paid via POST /orders/{orderId}/mark-paid.
        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(order));
    }

    /// <summary>Reads "Payment:DepositPercent" (50 means 50%) and falls back to 50 when absent/invalid.</summary>
    private decimal DepositPercent()
    {
        var raw = _configuration["Payment:DepositPercent"];
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
        order.DeliveredAt,
        order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ModelId,
            i.Quantity,
            i.Configuration.MaterialCode,
            i.Configuration.ColorCode,
            i.Configuration.LayerHeightMm,
            i.Configuration.InfillPercent,
            i.UnitPrice.Amount)).ToList());
}
