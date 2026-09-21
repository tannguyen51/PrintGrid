using MediatR;
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

    public PlaceOrderCommandHandler(
        IQuoteRepository quotes,
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _quotes = quotes;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
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

        var order = Order.CreateFromQuote(quote, address, orderNumber);
        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(order));
    }

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        order.TotalPrice.Amount,
        order.TotalPrice.Currency,
        order.PromisedDeliveryDate,
        order.CreatedAt,
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
