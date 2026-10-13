using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

public sealed record ConsolidatedShipmentResultDto(
    Guid ConsolidatedShipmentId,
    string TrackingNumber,
    IReadOnlyList<Guid> OrderIds,
    IReadOnlyList<string> OrderNumbers);

public sealed record ShipConsolidatedOrdersCommand(
    IReadOnlyList<Guid> OrderIds,
    string TrackingNumber) : IRequest<Result<ConsolidatedShipmentResultDto>>;

public sealed record DeliverConsolidatedOrdersCommand(
    IReadOnlyList<Guid> OrderIds) : IRequest<Result>;

public sealed record RefundOrderWithinScopeCommand(
    Guid OrderId,
    decimal Amount,
    string? Reason = null) : IRequest<Result>;

public class ConsolidateShipmentCommandHandler :
    IRequestHandler<ShipConsolidatedOrdersCommand, Result<ConsolidatedShipmentResultDto>>,
    IRequestHandler<DeliverConsolidatedOrdersCommand, Result>,
    IRequestHandler<RefundOrderWithinScopeCommand, Result>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ConsolidateShipmentCommandHandler(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<ConsolidatedShipmentResultDto>> Handle(
        ShipConsolidatedOrdersCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.TrackingNumber))
            return Result.Failure<ConsolidatedShipmentResultDto>(Error.Validation("Mã vận đơn không được để trống."));

        var distinctIds = command.OrderIds?.Distinct().ToList() ?? new List<Guid>();
        if (distinctIds.Count < 2)
            return Result.Failure<ConsolidatedShipmentResultDto>(Error.Validation("Cần chọn ít nhất 2 đơn hàng để đóng gói kiện gộp."));

        var loaded = new List<Order>();
        foreach (var id in distinctIds)
        {
            var order = await _orders.GetByIdAsync(id, cancellationToken);
            if (order is null)
                return Result.Failure<ConsolidatedShipmentResultDto>(Error.NotFound("Order", id));
            loaded.Add(order);
        }

        var anchor = loaded[0];
        if (anchor.Status != OrderStatus.QualityCheck)
            return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict($"Đơn {anchor.OrderNumber} không ở trạng thái sẵn sàng đóng gói (QualityCheck)."));

        for (var i = 1; i < loaded.Count; i++)
        {
            var other = loaded[i];
            if (other.Status != OrderStatus.QualityCheck)
                return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict($"Đơn {other.OrderNumber} không ở trạng thái sẵn sàng đóng gói (QualityCheck)."));

            if (other.CustomerId != anchor.CustomerId)
                return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict("Chỉ được gộp các đơn hàng của cùng một khách hàng."));

            if (!Equals(other.DeliveryAddress, anchor.DeliveryAddress))
                return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict("Chỉ được gộp các đơn hàng có cùng địa chỉ nhận hàng."));

            if (!anchor.CanConsolidateWith(other))
                return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict("Các đơn hàng phải sẵn sàng đóng gói trong vòng 24 giờ để gộp kiện."));
        }

        // Additional check: max spread across all orders in the batch <= 24h
        var readyTimes = loaded.Select(o => o.ReadyForPackingAtUtc ?? o.ConfirmedAt ?? o.CreatedAt).ToList();
        if ((readyTimes.Max() - readyTimes.Min()).TotalHours > 24.0)
            return Result.Failure<ConsolidatedShipmentResultDto>(Error.Conflict("Khoảng cách thời gian sẵn sàng giữa các đơn vượt quá 24 giờ."));

        var consolidatedId = Guid.NewGuid();
        var tracking = command.TrackingNumber.Trim();

        foreach (var order in loaded)
        {
            var shipResult = order.Ship(tracking, consolidatedId);
            if (shipResult.IsFailure)
                return Result.Failure<ConsolidatedShipmentResultDto>(shipResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var order in loaded)
        {
            await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        }

        return Result.Success(new ConsolidatedShipmentResultDto(
            consolidatedId,
            tracking,
            loaded.Select(o => o.Id).ToList(),
            loaded.Select(o => o.OrderNumber).ToList()));
    }

    public async Task<Result> Handle(DeliverConsolidatedOrdersCommand command, CancellationToken cancellationToken)
    {
        var distinctIds = command.OrderIds?.Distinct().ToList() ?? new List<Guid>();
        if (distinctIds.Count == 0)
            return Result.Failure(Error.Validation("Danh sách đơn cần xác nhận giao trống."));

        var loaded = new List<Order>();
        foreach (var id in distinctIds)
        {
            var order = await _orders.GetByIdAsync(id, cancellationToken);
            if (order is null)
                return Result.Failure(Error.NotFound("Order", id));
            loaded.Add(order);
        }

        foreach (var order in loaded)
        {
            var result = order.MarkDelivered();
            if (result.IsFailure)
                return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var order in loaded)
        {
            await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> Handle(RefundOrderWithinScopeCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", command.OrderId));

        var result = order.RefundWithinOriginalOrderScope(command.Amount);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

