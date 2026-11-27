using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>
/// Customer confirms or rejects COD (Cash on Delivery) receipt.
/// When order enters QualityCheck, customer must confirm within 24h that they will receive the order.
/// If rejected or timeout, deposit is forfeited per BR-PAY-010.
/// </summary>
public sealed record ConfirmCodReceiptCommand(
    Guid OrderId,
    bool Accepted,
    Guid? ConfirmedBy = null) : IRequest<Result>;

public sealed record RequestCodConfirmationCommand(
    Guid OrderId) : IRequest<Result>;

/// <summary>
/// Handler for customer to confirm COD receipt acceptance/rejection.
/// </summary>
public class ConfirmCodReceiptCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IPublisher publisher)
    : IRequestHandler<ConfirmCodReceiptCommand, Result>
{
    public async Task<Result> Handle(ConfirmCodReceiptCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null || command.ConfirmedBy is null || order.CustomerId != command.ConfirmedBy.Value)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        var previousStatus = order.Status;
        var previousConfirmation = order.IsCodConfirmed;
        var result = order.ConfirmCodReceipt(command.Accepted, command.ConfirmedBy);
        if (result.IsFailure)
            return result;

        if (order.Status == previousStatus && order.IsCodConfirmed == previousConfirmation) return Result.Success();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event for notification/timeline
        await publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId,
            command.Accepted ? "CodConfirmed" : "CodRejected"), cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Handler to request COD confirmation from customer (called when order enters QualityCheck).
/// Sets 24h deadline for customer to confirm.
/// </summary>
public class RequestCodConfirmationCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IPublisher publisher)
    : IRequestHandler<RequestCodConfirmationCommand, Result>
{
    public async Task<Result> Handle(RequestCodConfirmationCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        var previousCount = order.CodConfirmationRequestCount;
        var result = order.RequestCodConfirmation();
        if (result.IsFailure)
            return result;

        if (previousCount == order.CodConfirmationRequestCount) return Result.Success();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event for notification (customer needs to be notified)
        await publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId, "CodConfirmationRequested"), cancellationToken);

        return Result.Success();
    }
}
