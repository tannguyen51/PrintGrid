using MediatR;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class GetShipmentQueueQueryHandler : IRequestHandler<GetShipmentQueueQuery, Result<IReadOnlyList<ShipmentOrderDto>>>
{
    // The hub only acts on the tail of the chain: quality-checked (ready to ship), shipped, delivered.
    private static readonly OrderStatus[] _queueStatuses =
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
        var orders = await _orders.GetByStatusAsync(_queueStatuses, cancellationToken);

        // AC01 (FR-HUB-006 / BR-LOG-001): Group QualityCheck orders by same CustomerId + same DeliveryAddress
        // where ready timestamps are within <= 24 hours.
        var readyOrders = orders.Where(o => o.Status == OrderStatus.QualityCheck).ToList();
        var suggestionByOrderId = new Dictionary<Guid, (string GroupKey, IReadOnlyList<string> Companions)>();

        var visited = new HashSet<Guid>();
        var groupIndex = 1;
        foreach (var anchor in readyOrders)
        {
            if (visited.Contains(anchor.Id)) continue;

            var cluster = readyOrders
                .Where(candidate => candidate.Id == anchor.Id || anchor.CanConsolidateWith(candidate))
                .ToList();

            if (cluster.Count >= 2)
            {
                var groupKey = $"MERGE-{groupIndex:D3}";
                groupIndex++;
                foreach (var member in cluster)
                {
                    visited.Add(member.Id);
                    var companions = cluster
                        .Where(x => x.Id != member.Id)
                        .Select(x => x.OrderNumber)
                        .ToList();
                    suggestionByOrderId[member.Id] = (groupKey, companions);
                }
            }
        }

        var dtos = orders
            .Select(o =>
            {
                var suggestion = suggestionByOrderId.TryGetValue(o.Id, out var s)
                    ? s
                    : ((string? GroupKey, IReadOnlyList<string>? Companions))(null, null);

                return new ShipmentOrderDto(
                    o.Id,
                    o.OrderNumber,
                    o.Status.ToString(),
                    o.TrackingNumber,
                    o.PromisedDeliveryDate,
                    o.DeliveredAt,
                    o.DeliveredAt is not null,
                    o.CustomerId,
                    FormatAddress(o),
                    o.ReadyForPackingAtUtc ?? o.ConfirmedAt ?? o.CreatedAt,
                    o.ConsolidatedShipmentId,
                    suggestion.GroupKey,
                    suggestion.Companions,
                    o.TotalPrice.Amount,
                    o.RefundedAmount,
                    o.Items.Count);
            })
            .ToList();

        return Result.Success<IReadOnlyList<ShipmentOrderDto>>(dtos);
    }

    private static string FormatAddress(Domain.Entities.Order o)
    {
        var parts = new[]
        {
            o.DeliveryAddress.Street,
            o.DeliveryAddress.Ward,
            o.DeliveryAddress.District,
            o.DeliveryAddress.City
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
