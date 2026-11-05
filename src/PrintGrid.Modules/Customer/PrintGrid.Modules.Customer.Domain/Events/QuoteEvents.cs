using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

public record QuoteDraftedEvent : DomainEvent;

public record QuoteApprovedEvent : DomainEvent;
