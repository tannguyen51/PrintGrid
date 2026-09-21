using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    private readonly IOrderRepository _orders;

    public GetOrdersQueryHandler(IOrderRepository orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await _orders.GetByCustomerAsync(query.CustomerId, cancellationToken);
        var dtos = orders.Select(Map).ToList();
        return Result.Success<IReadOnlyList<OrderDto>>(dtos);
    }

    private static OrderDto Map(Domain.Entities.Order order) => new(
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