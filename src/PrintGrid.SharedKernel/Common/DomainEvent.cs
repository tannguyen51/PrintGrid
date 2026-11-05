using MediatR;

namespace PrintGrid.SharedKernel.Common;

public interface IDomainEvent : INotification;

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
