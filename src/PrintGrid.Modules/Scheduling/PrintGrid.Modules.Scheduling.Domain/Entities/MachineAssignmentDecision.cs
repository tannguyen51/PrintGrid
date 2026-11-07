using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// Logs each machine assignment decision with rejection reason for dispute resolution (doc §6.6).
/// Used by BR-07/BR-10 enforcement and assignment engine audit trail.
/// </summary>
public class MachineAssignmentDecision : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public Guid MachineId { get; private set; }
    public string Decision { get; private set; } = null!; // "assigned" | "rejected"
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private MachineAssignmentDecision() { }

    public static MachineAssignmentDecision Assign(Guid jobId, Guid machineId) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        MachineId = machineId,
        Decision = "assigned",
        RejectionReason = null
    };

    public static MachineAssignmentDecision Reject(Guid jobId, Guid machineId, string reason) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        MachineId = machineId,
        Decision = "rejected",
        RejectionReason = reason.Trim()
    };
}
