namespace PrintGrid.Modules.Customer.Application.DTOs;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateOnly? PromisedDeliveryDate,
    DateTime CreatedAt,
    decimal DepositAmount,
    decimal RefundedAmount,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    Guid Id,
    Guid ModelId,
    int Quantity,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    decimal InfillPercent,
    decimal UnitPrice,
    // Machine-binding / pinch profile fields propagated from quote (doc Sections 4-5)
    string? TechConstraintsJson = null,
    string ModelBindingKind = "không",
    Guid? PinnedModelId = null,
    string BindingLevel = "bắt_buộc",
    Guid? ReferenceOrderLineId = null);
