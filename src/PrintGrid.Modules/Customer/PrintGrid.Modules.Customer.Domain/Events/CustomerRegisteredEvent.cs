using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

public sealed record CustomerRegisteredEvent(Guid CustomerId, string Email) : DomainEvent;
