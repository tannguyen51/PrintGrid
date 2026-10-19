using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Domain.Entities;

/// <summary>
/// FR-CUST-013 / BR-IP-004 — design intake. A customer submits a brief plus up to five reference
/// images; ops triages the queue and moves the request through its lifecycle. The customer owns the
/// uploaded files, and no quote is created until ops attaches one (<see cref="QuoteId"/>).
/// </summary>
public sealed class DesignRequest : Entity<Guid>
{
    public static readonly string[] AllowedStatuses = ["Pending", "InReview", "Quoted", "Rejected", "Accepted"];

    private readonly List<string> _referenceImageKeys = new();

    public Guid CustomerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public Guid? QuoteId { get; private set; }
    public IReadOnlyList<string> ReferenceImageKeys => _referenceImageKeys.AsReadOnly();
    public DateTime CreatedAtUtc { get; private set; }

    private DesignRequest() { }

    public static DesignRequest Create(
        Guid customerId,
        string title,
        string description,
        IEnumerable<string>? referenceImageKeys = null)
    {
        var request = new DesignRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title.Trim(),
            Description = description.Trim(),
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        if (referenceImageKeys is not null)
        {
            request._referenceImageKeys.AddRange(
                referenceImageKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()));
        }

        return request;
    }

    /// <summary>Ops triage transition. Status is constrained to the documented intake lifecycle.</summary>
    public Result ChangeStatus(string status)
    {
        var next = AllowedStatuses.FirstOrDefault(s => s.Equals(status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (next is null)
            return Result.Failure(Error.Validation(
                $"Trạng thái '{status}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedStatuses)}."));

        Status = next;
        return Result.Success();
    }
}
