using System.Text.Json;
using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.Modules.Scheduling.Application.Services;

/// <summary>
/// Serialises the decision snapshot stored on <see cref="Domain.Entities.AssignmentDecision"/>
/// (FR-SCHED-009). The log has to reproduce a decision exactly as it was made, so the shapes
/// below are the audit format — treat them as a contract, not an implementation detail.
/// </summary>
public static class DecisionSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Candidates(CapabilityFilterResult filter, Guid? excludedLabId = null) =>
        JsonSerializer.Serialize(new
        {
            // Recorded so the trace explains a candidate set that is smaller than capability
            // alone would allow (FR-SCHED-009: every decision must be reconstructible).
            excludedLabId,
            capable = filter.Candidates.Select(c => new
            {
                labId = c.Lab.Id,
                labName = c.Lab.Name,
                machineId = c.Machine.Id,
                machineName = c.Machine.Name,
                speedFactor = c.Machine.SpeedFactor
            }),
            rejected = filter.Rejections.Select(r => new
            {
                machineId = r.MachineId,
                reason = r.Reason
            })
        }, Options);

    public static string Ranking(IReadOnlyList<ScoredPlacement> ranked) =>
        JsonSerializer.Serialize(ranked.Select(s => new
        {
            labId = s.Placement.Candidate.Lab.Id,
            machineId = s.Placement.Candidate.Machine.Id,
            plannedStartUtc = s.Placement.PlannedStartUtc,
            plannedEndUtc = s.Placement.PlannedEndUtc,
            estimatedCost = s.Placement.EstimatedCost,
            machineUtilization = s.Placement.MachineUtilization,
            score = s.Score,
            breakdown = s.Breakdown
        }), Options);

    /// <summary>Freezes the effective weights ("kỳ cấu hình") alongside the decision (BR-CONFIG-003).</summary>
    public static string ScoringConfig(ScoringParameterSet set) =>
        JsonSerializer.Serialize(new
        {
            version = set.Version,
            weights = new
            {
                dueDateSlack = set.Weights.DueDateSlack,
                labReliability = set.Weights.LabReliability,
                loadBalance = set.Weights.LoadBalance,
                cost = set.Weights.Cost,
                transit = set.Weights.Transit
            }
        }, Options);
}
