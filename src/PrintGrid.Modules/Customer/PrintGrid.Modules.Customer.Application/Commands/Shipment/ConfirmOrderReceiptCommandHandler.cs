using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public class ConfirmOrderReceiptCommandHandler : IRequestHandler<ConfirmOrderReceiptCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ConfirmOrderReceiptCommandHandler(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(ConfirmOrderReceiptCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        // Mirror the other customer endpoints: an order that is not yours reads as "not found".
        if (order is null || order.CustomerId != command.CustomerId)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        var result = order.ConfirmReceipt(command.CustomerId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The warranty anchor has moved — refresh the customer timeline one last time.
        await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        return Result.Success();
    }
}
