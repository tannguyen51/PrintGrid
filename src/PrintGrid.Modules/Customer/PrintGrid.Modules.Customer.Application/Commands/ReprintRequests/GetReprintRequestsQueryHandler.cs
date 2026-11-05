using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public class GetReprintRequestsQueryHandler(IOrderRepository orders, IReprintRequestRepository requests)
    : IRequestHandler<GetReprintRequestsQuery, Result<IReadOnlyList<ReprintRequestDto>>>
{
    public async Task<Result<IReadOnlyList<ReprintRequestDto>>> Handle(GetReprintRequestsQuery query, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null || order.CustomerId != query.CustomerId)
            return Result.Failure<IReadOnlyList<ReprintRequestDto>>(Error.NotFound("Order", query.OrderId));
        var requests1 = await requests.GetByOrderAsync(query.OrderId, query.CustomerId, cancellationToken);
        return Result.Success<IReadOnlyList<ReprintRequestDto>>([
            .. requests1.Select(CreateReprintRequestCommandHandler.Map)
        ]);
    }
}
