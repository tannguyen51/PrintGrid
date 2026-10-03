using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

/// <summary>
/// Electronic evidence that a customer's model file entered the platform (BR-IP-001/002).
/// Consumed by the notification handler which emails the customer the time/size/hash receipt.
/// </summary>
public sealed record ModelUploadedEvent(
    Guid CustomerId,
    Guid ModelId,
    string FileName,
    string StorageKey,
    string Sha256,
    long SizeBytes) : DomainEvent;
