using MediatR;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class GetShipmentQueueQueryHandler : IRequestHandler<GetShipmentQueueQuery, Result<IReadOnlyList<ShipmentOrderDto>>>
{
    // The hub only acts on the tail of the chain: quality-checked (ready to ship), shipped, delivered.
    private static readonly OrderStatus[] QueueStatuses =
    [
        OrderStatus.QualityCheck,
        OrderStatus.Shipping,
        OrderStatus.Delivered
    ];

    private readonly IOrderRepository _orders;

    public GetShipmentQueueQueryHandler(IOrderRepository orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<ShipmentOrderDto>>> Handle(
        GetShipmentQueueQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetByStatusAsync(QueueStatuses, cancellationToken);
        var dtos = orders.Select(Map).ToList();
        return Result.Success<IReadOnlyList<ShipmentOrderDto>>(dtos);
    }

    private static ShipmentOrderDto Map(Domain.Entities.Order o) => new(
        o.Id,
        o.OrderNumber,
        o.Status.ToString(),
        o.TrackingNumber,
        o.PromisedDeliveryDate,
        o.DeliveredAt,
        o.DeliveredAt is not null);
}
