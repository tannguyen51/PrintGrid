using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>
/// Shared goods-out gate for every path that moves an order into <c>Shipping</c>: a fully refunded
/// order can no longer leave the hub, and every item must have completed the hub QC gate
/// (<c>Completed</c> — job done, hub inspection passed and proof approved). "QualityCheck" is only
/// a request for inspection and is deliberately NOT enough.
/// </summary>
internal static class ShipmentGate
{
    internal const string CompletedItemStatus = "Completed";

    internal static Result EnsureReadyToLeaveHub(Order order)
    {
        if (order.IsFullyRefunded)
        {
            return Result.Failure(Error.Conflict(
                $"Đơn {order.OrderNumber} đã được hoàn tiền toàn phần " +
                $"(đã thu {order.PaidAmount:N0} {order.TotalPrice.Currency}, đã hoàn {order.RefundedAmount:N0} {order.TotalPrice.Currency}), không thể xuất kho."));
        }

        var notCompleted = order.Items
            .Where(i => !string.Equals(i.ItemStatus, CompletedItemStatus, StringComparison.Ordinal))
            .Select(i => $"{i.Id} ({i.ItemStatus})")
            .ToList();

        if (notCompleted.Count > 0)
        {
            return Result.Failure(Error.Conflict(
                $"Đơn {order.OrderNumber} còn {notCompleted.Count} item chưa hoàn tất QC: {string.Join(", ", notCompleted)}. " +
                $"Chỉ xuất kho khi mọi item ở trạng thái {CompletedItemStatus}."));
        }

        return Result.Success();
    }
}

/// <summary>
/// Kill switch for the goods-out business rules (settlement, full refund, per-item QC completion).
/// Rules are enforced by default; set <c>Shipping:Gates:Enforce=false</c> (environment variable
/// <c>Shipping__Gates__Enforce</c>) to waive them temporarily, e.g. for a demo. The order state machine
/// (only <c>QualityCheck → Shipping</c>) and the mandatory tracking number are deliberately NOT part of
/// this switch — bypassing them would corrupt order history and leave shipped orders without a waybill.
/// </summary>
internal static class ShippingGateSettings
{
    internal const string EnforceKey = "Shipping:Gates:Enforce";

    internal static bool IsEnforced(IConfiguration configuration) =>
        !bool.TryParse(configuration[EnforceKey], out var enforce) || enforce;
}

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShipOrderCommandHandler> _logger;

    public ShipOrderCommandHandler(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IConfiguration configuration,
        ILogger<ShipOrderCommandHandler> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        if (ShippingGateSettings.IsEnforced(_configuration))
        {
            // FR-CUST-012 AC02 / BR-PAY-008: goods may not leave the hub until the order is fully settled.
            if (!order.IsSettled)
            {
                var shortfall = order.TotalPrice.Amount - order.PaidAmount;
                return Result.Failure(Error.Conflict(
                    $"Đơn {order.OrderNumber} chưa tất toán đủ — còn thiếu {shortfall:N0} {order.TotalPrice.Currency}, không thể xuất kho."));
            }

            // Fully-refunded orders are dead, and no item may skip the QC-completed gate.
            var gate = ShipmentGate.EnsureReadyToLeaveHub(order);
            if (gate.IsFailure)
                return gate;
        }
        else
        {
            _logger.LogWarning(
                "Shipping gates are disabled (Shipping:Gates:Enforce=false) — order {OrderNumber} ships " +
                "without the settlement / full-refund / item-QC checks.",
                order.OrderNumber);
        }

        // Still enforced with the gates off: a tracking number is required and only QualityCheck may ship.
        var result = order.Ship(command.TrackingNumber);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Customer-visible: push a timeline refresh so the order shows "đang giao" (Shipping).
        await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        return Result.Success();
    }
}
