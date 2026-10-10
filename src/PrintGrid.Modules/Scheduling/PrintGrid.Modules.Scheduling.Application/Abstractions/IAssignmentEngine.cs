using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

/// <summary>What caused an assignment decision — stored on every decision log entry (FR-SCHED-009).</summary>
public static class AssignmentTriggers
{
    public const string InitialAssign = "initial_assign";
    public const string LabDecline = "lab_decline";
    public const string PrintFailure = "print_failure";
    public const string UrgentReprint = "urgent_reprint";
    public const string OpsOverride = "ops_override";
    public const string DateChangeProbe = "date_change_probe";
}

/// <summary>Terminal states of one engine run, as recorded in the decision log.</summary>
public static class AssignmentOutcomes
{
    public const string Assigned = "assigned";
    public const string NoCapableMachine = "no_capable_machine";
    public const string NoFeasibleSlot = "no_feasible_slot";
    public const string PlacementRace = "placement_race";
    public const string RejectedByAggregate = "rejected_by_aggregate";
    public const string Probe = "probe";
}

/// <summary>
/// One engine run. <paramref name="Trigger"/> is recorded verbatim, so
/// <see cref="Domain.Events.ReschedulingTriggeredEvent.Trigger"/> values ("lab_decline",
/// "print_failure") flow straight into the audit trail.
/// <paramref name="ExcludeLabId"/> is the party responsible for the event: a lab that
/// declined must not be offered the same job again (FR-SCHED-007 "xử lý từ-chối"). It is a
/// preference, not an absolute — if it would leave no candidate at all, the engine keeps the
/// lab rather than lose feasibility (BR-SCHED-006).
/// </summary>
public sealed record AssignmentRequest(
    string Trigger,
    string ActorType = "system",
    string? ActorId = null,
    TimeSpan? TimeBudget = null,
    bool IgnoreDueDate = false,
    bool ProbeOnly = false,
    Guid? ExcludeLabId = null);

/// <summary>
/// Result of one engine run: the placement (when committed), the full ranking that led to
/// it, and the measurements PERF-002 asks for.
/// </summary>
public sealed record AssignmentOutcome(
    bool Assigned,
    AssignmentResultDto? Result,
    string Outcome,
    string? Reason,
    IReadOnlyList<ScoredPlacement> Ranking,
    IReadOnlyList<CapabilityRejection> Rejections,
    int ElapsedMs,
    bool BudgetExceeded,
    DateTime? EarliestFeasibleEndUtc);

/// <summary>
/// The assign engine as a reusable seam (FR-SCHED-006/007/008b): manual assignment, the
/// event-driven rescheduler and urgent reprints all run through this one implementation,
/// and every run leaves exactly one append-only decision log entry.
/// </summary>
public interface IAssignmentEngine
{
    Task<AssignmentOutcome> AssignAsync(
        Job job,
        AssignmentRequest request,
        CancellationToken cancellationToken = default);
}
