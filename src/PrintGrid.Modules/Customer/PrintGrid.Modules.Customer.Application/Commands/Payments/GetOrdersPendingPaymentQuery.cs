using MediatR;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Payments;

/// <summary>One PaymentPending order as the ops deposit-collection screen needs it.</summary>
public sealed record PendingOrderPaymentDto(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalPrice,
    decimal DepositAmount,
    decimal PaidAmount,
    decimal Remaining,
    DateTime CreatedAt,
    DateOnly PromisedDeliveryDate,
    string Currency);

/// <summary>
/// FR-CUST-012 — ops queue of orders still waiting for their deposit. Optional <paramref name="Search"/>
/// matches the order number or customer id.
/// </summary>
public sealed record GetOrdersPendingPaymentQuery(string? Search = null)
    : IRequest<Result<IReadOnlyList<PendingOrderPaymentDto>>>;

public sealed class GetOrdersPendingPaymentQueryHandler
    : IRequestHandler<GetOrdersPendingPaymentQuery, Result<IReadOnlyList<PendingOrderPaymentDto>>>
{
    private static readonly OrderStatus[] _pendingStatuses = [OrderStatus.PaymentPending];

    private readonly IOrderRepository _orders;

    public GetOrdersPendingPaymentQueryHandler(IOrderRepository orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<PendingOrderPaymentDto>>> Handle(
        GetOrdersPendingPaymentQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetByStatusAsync(_pendingStatuses, cancellationToken);

        var search = request.Search?.Trim();
        var filtered = string.IsNullOrWhiteSpace(search)
            ? orders
            : orders.Where(o =>
                    o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var dtos = filtered
            .Select(o => new PendingOrderPaymentDto(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.TotalPrice.Amount,
                o.DepositAmount,
                o.PaidAmount,
                Math.Max(0m, o.TotalPrice.Amount - o.PaidAmount),
                o.CreatedAt,
                o.PromisedDeliveryDate,
                o.TotalPrice.Currency))
            .ToList();

        return Result.Success<IReadOnlyList<PendingOrderPaymentDto>>(dtos);
    }
}
