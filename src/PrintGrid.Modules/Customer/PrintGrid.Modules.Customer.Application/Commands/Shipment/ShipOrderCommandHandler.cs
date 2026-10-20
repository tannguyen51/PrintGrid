using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ShipOrderCommandHandler(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        // FR-CUST-012 AC02 / BR-PAY-008: goods may not leave the hub until the order is fully settled.
        if (!order.IsSettled)
        {
            var shortfall = order.TotalPrice.Amount - order.PaidAmount;
            return Result.Failure(Error.Conflict(
                $"Đơn {order.OrderNumber} chưa tất toán đủ — còn thiếu {shortfall:N0} {order.TotalPrice.Currency}, không thể xuất kho."));
        }

        var result = order.Ship(command.TrackingNumber);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Customer-visible: push a timeline refresh so the order shows "đang giao" (Shipping).
        await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        return Result.Success();
    }
}
