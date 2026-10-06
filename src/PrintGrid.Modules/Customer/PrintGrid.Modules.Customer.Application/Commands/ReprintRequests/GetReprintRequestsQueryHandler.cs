using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public class GetReprintRequestsQueryHandler : IRequestHandler<GetReprintRequestsQuery, Result<IReadOnlyList<ReprintRequestDto>>>
{
    private readonly IOrderRepository _orders;
    private readonly IReprintRequestRepository _requests;
    public GetReprintRequestsQueryHandler(IOrderRepository orders, IReprintRequestRepository requests)
    { _orders = orders; _requests = requests; }

    public async Task<Result<IReadOnlyList<ReprintRequestDto>>> Handle(GetReprintRequestsQuery query, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null || order.CustomerId != query.CustomerId)
            return Result.Failure<IReadOnlyList<ReprintRequestDto>>(Error.NotFound("Order", query.OrderId));
        var requests = await _requests.GetByOrderAsync(query.OrderId, query.CustomerId, cancellationToken);
        return Result.Success<IReadOnlyList<ReprintRequestDto>>(requests.Select(CreateReprintRequestCommandHandler.Map).ToList());
    }
}
