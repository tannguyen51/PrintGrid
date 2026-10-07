using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public record GetOrderTimelineQuery(Guid OrderId, Guid CustomerId) : IRequest<Result<CustomerTimelineDto>>;

public class GetOrderTimelineQueryHandler : IRequestHandler<GetOrderTimelineQuery, Result<CustomerTimelineDto>>
{
    private readonly IOrderRepository _orders;

    public GetOrderTimelineQueryHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<Result<CustomerTimelineDto>> Handle(GetOrderTimelineQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null || order.CustomerId != request.CustomerId)
        {
            return Result.Failure<CustomerTimelineDto>(Error.NotFound("Order", request.OrderId));
        }

        // The hub's "Delivered" is only the physical handover; the warranty anchor (DeliveredAt) is
        // stamped by the CUSTOMER's receipt confirmation. So the last two stages split on that flag.
        var handedOver = order.Status == OrderStatus.Delivered && order.DeliveredAt is null;
        var receivedByCustomer = order.Status == OrderStatus.Delivered && order.DeliveredAt is not null;

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
            "Model_" + i.ModelId.ToString().Substring(0, 4), // Since ModelName is not directly in OrderItem, we mock it or fetch it. Actually the FE might not need the exact model name or we just provide the ID.
            i.Quantity,
            i.ItemStatus
        )).ToList();

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
            CanConfirmReceipt: handedOver
        );

        return Result.Success(dto);
    }
}
