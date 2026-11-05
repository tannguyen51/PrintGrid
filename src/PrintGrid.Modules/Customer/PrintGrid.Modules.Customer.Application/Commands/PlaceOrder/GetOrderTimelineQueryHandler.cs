using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public record GetOrderTimelineQuery(Guid OrderId, Guid CustomerId) : IRequest<Result<CustomerTimelineDto>>;

public class GetOrderTimelineQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderTimelineQuery, Result<CustomerTimelineDto>>
{
    public async Task<Result<CustomerTimelineDto>> Handle(GetOrderTimelineQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null || order.CustomerId != request.CustomerId)
        {
            return Result.Failure<CustomerTimelineDto>(Error.NotFound("Order", request.OrderId));
        }

        // The hub's "Delivered" is only the physical handover; the warranty anchor (DeliveredAt) is
        // stamped by the CUSTOMER's receipt confirmation. So the last two stages split on that flag.
        var handedOver = order is { Status: OrderStatus.Delivered, DeliveredAt: null };
        var receivedByCustomer = order is { Status: OrderStatus.Delivered, DeliveredAt: not null };

        var stages = new List<OrderStageDto>
        {
            new("Payment Pending", order.CreatedAt, order.Status == OrderStatus.PaymentPending),
            new("Confirmed", order.ConfirmedAt, order.Status == OrderStatus.Confirmed),
            new("In Production", null, order.Status == OrderStatus.InProduction),
            new("Quality Check", null, order.Status == OrderStatus.QualityCheck),
            new("Shipping", null, order.Status == OrderStatus.Shipping),
            new("Delivered", null, handedOver),
            new("Received by Customer", order.DeliveredAt, receivedByCustomer)
        };

        var currentStageName = stages.FirstOrDefault(s => s.IsCurrent)?.StageName ?? order.Status.ToString();

        var itemDtos = order.Items.Select(i => new CustomerOrderItemDto(
            i.Id,
            "Model_" + i.ModelId.ToString()[..4],
            i.Quantity,
            i.ItemStatus)).ToList();

        var dto = new CustomerTimelineDto(
            order.Id,
            order.OrderNumber,
            currentStageName,
            order.PromisedDeliveryDate,
            order.IsDelayed,
            stages,
            itemDtos,
            order.TrackingNumber,
            order.DeliveredAt,
            CanConfirmReceipt: handedOver,
            ContractNumber: order.ContractNumber);

        return Result.Success(dto);
    }
}
