using PrintGrid.Modules.Customer.Domain.Enums;

namespace PrintGrid.Modules.Customer.Application.DTOs;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateOnly PromisedDeliveryDate,
    DateTime CreatedAt,
    IReadOnlyCollection<OrderItemDto> Items);

public record OrderItemDto(
    Guid Id,
    Guid ModelId,
    int Quantity,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int InfillPercent,
    decimal UnitPrice);
