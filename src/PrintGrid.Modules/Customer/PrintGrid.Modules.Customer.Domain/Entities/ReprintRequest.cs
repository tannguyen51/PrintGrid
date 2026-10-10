using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class ReprintRequest : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string[] Photos { get; private set; } = [];
    public ReprintRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ResolutionNote { get; private set; }

    private ReprintRequest() { }

    public static ReprintRequest Create(
        Guid orderId, Guid customerId, string reason, string description, IEnumerable<string> photos)
    {
        return new ReprintRequest
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Reason = reason.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            Photos = photos.ToArray(),
            Status = ReprintRequestStatus.UnderReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
