namespace PrintGrid.Modules.Customer.Application.DTOs;

public record CustomerTimelineDto(
    Guid OrderId,
    string OrderNumber,
    string CurrentStage,
    DateOnly PromisedDeliveryDate,
    bool IsDelayed,
    IReadOnlyCollection<OrderStageDto> Stages,
    IReadOnlyCollection<CustomerOrderItemDto> Items
);

public record OrderStageDto(
    string StageName,
    DateTime? CompletedAt,
    bool IsCurrent
);

public record CustomerOrderItemDto(
    Guid Id,
    string ModelName,
    int Quantity,
    string Status
);
