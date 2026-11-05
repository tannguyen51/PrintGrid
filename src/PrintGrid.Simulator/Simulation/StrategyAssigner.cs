using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.Simulator.Simulation;

public enum SchedulingStrategy
{
    Capacity,
    Edd,
    Nearest,
    Random
}

public sealed record AssignerProposal(
    Guid LabId,
    Guid MachineId,
    DateTime StartUtc,
    DateTime EndUtc,
    decimal Score,
    decimal Utilization,
    int CandidateCount);

/// <summary>
/// The in-process assigner. It reuses the REAL <see cref="CapabilityFilter"/>,
/// <see cref="AssignmentScorer"/> and <see cref="ScoringWeights"/>; only the timeline loop
/// (earliest free slot + utilization against <see cref="MachineCalendar"/>) is implemented
/// here, mirroring <c>AssignmentEngine.AssignAsync</c>. Each strategy is a different weight
/// mix on the same scorer, so the comparison measures strategy, not four implementations.
/// </summary>
public sealed class StrategyAssigner(IReadOnlyList<Lab> labs, MachineCalendar calendar)
{
    public AssignerProposal? TryAssign(
        Job job,
        DateTime notBeforeUtc,
        SchedulingStrategy strategy,
        Random strategyRandom,
        Guid? excludeLabId = null)
    {
        var filtered = CapabilityFilter.Filter(job.Specification, labs);
        var candidates = filtered.Candidates;

        if (excludeLabId is { } excluded)
        {
            // BR-SCHED-006: exclusion is a preference; the only capable lab stays as last resort.
            var remaining = candidates.Where(c => c.Lab.Id != excluded).ToList();
            if (remaining.Count > 0) candidates = remaining;
        }

        if (candidates.Count == 0) return null;

        var proposals = new List<PlacementProposal>(candidates.Count);
        proposals.AddRange(from candidate in candidates let duration = Math.Max(1.0, job.EstimatedPrintMinutes / (double)candidate.Machine.SpeedFactor) let start = calendar.FindEarliestFreeSlot(candidate.Machine.Id, duration, notBeforeUtc) let end = start.AddMinutes(duration) let horizonEnd = end > notBeforeUtc.AddDays(7) ? end : notBeforeUtc.AddDays(7) let utilization = calendar.GetUtilization(candidate.Machine.Id, notBeforeUtc, horizonEnd) let cost = job.Specification.MaterialGrams * 0.5m + (decimal)duration * 0.1m select new PlacementProposal(candidate, start, end, cost, utilization));

        var scorer = new AssignmentScorer(WeightsFor(strategy));
        var ranked = scorer.Rank(job, proposals, proposals.Max(p => p.EstimatedCost));

        // Deterministic tie-break. A single-criterion strategy (nearest = transit only) ties
        // EVERY lab in the same city, and LINQ's stable sort would then hand all work to
        // whichever lab the filter happened to list first — a starved-network artifact that
        // says nothing about the strategy. Equal scores go to the earliest finish instead.
        if (strategy != SchedulingStrategy.Random)
        {
            ranked =
            [
                .. ranked
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.Placement.PlannedEndUtc)
            ];
        }

        var (placement, score, _) = strategy == SchedulingStrategy.Random
            ? ranked[strategyRandom.Next(ranked.Count)]
            : ranked[0];

        calendar.Book(placement.Candidate.Machine.Id, placement.PlannedStartUtc, placement.PlannedEndUtc);

        return new AssignerProposal(
            placement.Candidate.Lab.Id,
            placement.Candidate.Machine.Id,
            placement.PlannedStartUtc,
            placement.PlannedEndUtc,
            score,
            placement.MachineUtilization,
            candidates.Count);
    }

    private static ScoringWeights WeightsFor(SchedulingStrategy strategy) => strategy switch
    {
        // capacity: least loaded machine first.
        SchedulingStrategy.Capacity => new ScoringWeights
        {
            DueDateSlack = 0m, LabReliability = 0m, LoadBalance = 1m, Cost = 0m, Transit = 0m
        },
        // edd: maximum due-date slack = earliest feasible end.
        SchedulingStrategy.Edd => new ScoringWeights
        {
            DueDateSlack = 1m, LabReliability = 0m, LoadBalance = 0m, Cost = 0m, Transit = 0m
        },
        // nearest: shortest transit to the hub.
        SchedulingStrategy.Nearest => new ScoringWeights
        {
            DueDateSlack = 0m, LabReliability = 0m, LoadBalance = 0m, Cost = 0m, Transit = 1m
        },
        // random: real default weights, then a uniform pick among all feasible placements.
        _ => ScoringWeights.Default
    };
}
