using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public DeliverOrderCommandHandler(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(DeliverOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        var result = order.MarkDelivered();
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Customer-visible: push a timeline refresh so the order shows "đã giao" and the
        // customer is invited to confirm receipt. DeliveredAt (warranty anchor) stays null.
        await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        return Result.Success();
    }
}
