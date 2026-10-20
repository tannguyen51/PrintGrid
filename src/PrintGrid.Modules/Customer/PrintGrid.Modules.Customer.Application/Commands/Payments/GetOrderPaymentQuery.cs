using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Payments;

/// <summary>FR-CUST-012: the customer's own deposit/settlement overview for one order.</summary>
public sealed record GetOrderPaymentQuery(Guid OrderId, Guid CustomerId) : IRequest<Result<OrderPaymentDto>>;

public sealed class GetOrderPaymentQueryHandler : IRequestHandler<GetOrderPaymentQuery, Result<OrderPaymentDto>>
{
    private readonly IOrderRepository _orders;

    public GetOrderPaymentQueryHandler(IOrderRepository orders) => _orders = orders;

    public async Task<Result<OrderPaymentDto>> Handle(GetOrderPaymentQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.CustomerId != request.CustomerId)
            return Result.Failure<OrderPaymentDto>(Error.NotFound("Order", request.OrderId));

        return Result.Success(PaymentMapping.ToDto(order));
    }
}
