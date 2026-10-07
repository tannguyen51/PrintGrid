using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

public record QuoteDraftedEvent(
    Guid QuoteId,
    decimal DraftPrice,
    DateOnly DraftDeliveryDate,
    string? PlacementBasis) : DomainEvent;

public record QuoteApprovedEvent(
    Guid QuoteId,
    Guid? StaffId,
    decimal EnginePrice,
    decimal PublishedPrice,
    DateOnly EngineDeliveryDate,
    DateOnly PublishedDeliveryDate,
    string? Reason,
    bool AutoApproved,
    DateTime PublishedAt) : DomainEvent;
