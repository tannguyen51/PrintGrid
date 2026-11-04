using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// Membership of a staff account in a lab. <see cref="Lab.OwnerUserId"/> covers the
/// manager who owns the lab; this table covers the rest of the team (operators, and a
/// second manager if one is ever added). Every lab-scoped query resolves the caller's
/// lab either through ownership or through this membership — never through a fallback
/// (BR-ACCESS-003/004: access is always scoped to exactly one lab).
/// </summary>
public class LabStaff : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private LabStaff() { }

    internal static LabStaff Create(Guid labId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        LabId = labId,
        UserId = userId,
        CreatedAtUtc = DateTime.UtcNow
    };
}
