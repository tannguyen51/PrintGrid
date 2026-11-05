using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public class GetOrdersQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders1 = await orders.GetByCustomerAsync(query.CustomerId, cancellationToken);
        var dtos = orders1.Select(Map).ToList();
        return Result.Success<IReadOnlyList<OrderDto>>(dtos);
    }

    private static OrderDto Map(Domain.Entities.Order order) => new(
        order.Id,
        order.Status.ToString());
}
