using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.Services;

/// <summary>
/// The single assign-engine implementation behind manual assignment (FR-SCHED-006),
/// event-driven rescheduling (FR-SCHED-007) and urgent reprints (FR-HUB-003).
///
/// Invariants it upholds:
/// <list type="bullet">
/// <item>every capable candidate is filtered, scored and ranked, and the whole ranking is
/// written to the decision log in the same transaction as the placement (FR-SCHED-009 E1);</item>
/// <item>the job itself must be Pending/Reassigned — jobs already printing are never touched
/// (BR-SCHED-007), and this engine never modifies any OTHER job;</item>
/// <item>the DB EXCLUDE constraint on scheduling.jobs is the final arbiter against concurrent
/// placers, so the committed schedule is feasible by construction (NFR-REL-004);</item>
/// <item>the best-ranked placement is always attempted once, so a feasible fallback is
/// committed even when the time budget runs out (BR-SCHED-005/006).</item>
/// </list>
/// </summary>
public class AssignmentEngine : IAssignmentEngine
{
    /// <summary>BR-SCHED-005 / NFR-PERF-002: repair ≤ 30 s hard, ≤ 15 s mean.</summary>
    public const int DefaultTimeBudgetSeconds = 30;

    private readonly IJobRepository _jobs;
    private readonly ILabRepository _labs;
    private readonly CapabilityFilter _capabilityFilter;
    private readonly AssignmentScorer _scorer;
    private readonly IMachineTimelineService _timeline;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssignmentDecisionRepository _decisions;
    private readonly ScoringParameterSet _scoringConfig;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AssignmentEngine> _logger;

    public AssignmentEngine(
        IJobRepository jobs,
        ILabRepository labs,
        CapabilityFilter capabilityFilter,
        AssignmentScorer scorer,
        IMachineTimelineService timeline,
        IUnitOfWork unitOfWork,
        IAssignmentDecisionRepository decisions,
        ScoringParameterSet scoringConfig,
        IDateTimeProvider clock,
        ILogger<AssignmentEngine> logger)
    {
        _jobs = jobs;
        _labs = labs;
        _capabilityFilter = capabilityFilter;
        _scorer = scorer;
        _timeline = timeline;
        _unitOfWork = unitOfWork;
        _decisions = decisions;
        _scoringConfig = scoringConfig;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AssignmentOutcome> AssignAsync(
        Job job,
        AssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var budget = request.TimeBudget ?? TimeSpan.FromSeconds(DefaultTimeBudgetSeconds);
        var stopwatch = Stopwatch.StartNew();

        var labs = await _labs.GetActiveWithMachinesAsync(cancellationToken);
        var filter = ApplyExclusion(
            _capabilityFilter.Filter(job.Specification, labs), request.ExcludeLabId, job.Id);

        if (filter.Candidates.Count == 0)
        {
            _logger.LogWarning(
                "Job {JobId} has no capable machine; {RejectionCount} machines rejected",
                job.Id, filter.Rejections.Count);

            return await RecordFailureAsync(
                job, request, filter, [], AssignmentOutcomes.NoCapableMachine,
                "No lab in the network can satisfy this job specification",
                stopwatch, budget, budgetExceeded: false, earliestEnd: null, cancellationToken);
        }

        var windowEnd = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var proposals = new List<PlacementProposal>();
        DateTime? earliestEnd = null;

        foreach (var candidate in filter.Candidates)
        {
            var start = await _timeline.FindEarliestFreeSlotAsync(
                candidate.Machine, job.EstimatedPrintMinutes, _clock.UtcNow, cancellationToken);
            var duration = job.EstimatedPrintMinutes / candidate.Machine.SpeedFactor;
            var end = start.AddMinutes((double)duration);

            if (earliestEnd is null || end < earliestEnd.Value)
                earliestEnd = end;

            // Feasibility is judged against the internal due date — unless we are probing what
            // the network could do if the committed date moved (BR-SCHED-011).
            if (!request.IgnoreDueDate && end > windowEnd)
                continue;

            var utilization = await _timeline.GetUtilizationAsync(
                candidate.Machine, _clock.UtcNow, windowEnd, cancellationToken);
            var cost = job.Specification.MaterialGrams * 0.5m + (decimal)duration * 0.1m;

            proposals.Add(new PlacementProposal(candidate, start, end, cost, utilization));
        }

        IReadOnlyList<ScoredPlacement> ranked = proposals.Count == 0
            ? []
            : _scorer.Rank(job, proposals, proposals.Max(p => p.EstimatedCost));

        if (request.ProbeOnly)
        {
            // A probe only answers "when could we realistically deliver?". It must never
            // place work past a committed date, so it writes nothing at all.
            return new AssignmentOutcome(
                false, null, AssignmentOutcomes.Probe, null, ranked, filter.Rejections,
                Elapsed(stopwatch), BudgetExceeded: false, earliestEnd);
        }

        if (ranked.Count == 0)
        {
            return await RecordFailureAsync(
                job, request, filter, [], AssignmentOutcomes.NoFeasibleSlot,
                "No capable machine has capacity before the internal due date",
                stopwatch, budget, budgetExceeded: false, earliestEnd, cancellationToken);
        }

        var budgetExceeded = false;

        // Try candidates best-score-first. On a DB-level collision the tracked changes are
        // discarded, the job is re-read fresh, and the next candidate is attempted.
        for (var index = 0; index < ranked.Count; index++)
        {
            // BR-SCHED-006: the best-ranked placement is always attempted at least once, so a
            // feasible fallback gets committed even when the budget is already spent; the
            // budget only stops the search for further candidates (BR-SCHED-005).
            if (index > 0 && stopwatch.Elapsed > budget)
            {
                budgetExceeded = true;
                break;
            }

            var best = ranked[index];

            var assignment = job.AssignTo(
                best.Placement.Candidate.Lab.Id,
                best.Placement.Candidate.Machine.Id,
                best.Placement.PlannedStartUtc,
                best.Placement.PlannedEndUtc,
                best.Score);

            if (assignment.IsFailure)
            {
                return await RecordFailureAsync(
                    job, request, filter, ranked, AssignmentOutcomes.RejectedByAggregate,
                    assignment.Error.Message, stopwatch, budget, budgetExceeded, earliestEnd, cancellationToken);
            }

            // FR-SCHED-009 E1: the decision entry rides in the SAME transaction as the
            // placement it explains — if the log cannot be written, neither is the assignment.
            await _decisions.AddAsync(
                BuildDecision(job, request, AssignmentOutcomes.Assigned, filter, ranked, stopwatch, budget, budgetExceeded, best),
                cancellationToken);

            if (await _unitOfWork.TrySaveChangesSafeAsync(cancellationToken))
            {
                _logger.LogInformation(
                    "Job {JobId} assigned to lab {LabId} machine {MachineId} with score {Score} (trigger {Trigger}, {ElapsedMs} ms)",
                    job.Id, job.LabId, job.MachineId, best.Score, request.Trigger, Elapsed(stopwatch));

                return new AssignmentOutcome(
                    true,
                    new AssignmentResultDto(
                        job.Id,
                        best.Placement.Candidate.Lab.Id,
                        best.Placement.Candidate.Machine.Id,
                        best.Placement.PlannedStartUtc,
                        best.Placement.PlannedEndUtc,
                        best.Score,
                        best.Breakdown),
                    AssignmentOutcomes.Assigned, null, ranked, filter.Rejections,
                    Elapsed(stopwatch), budgetExceeded, earliestEnd);
            }

            _logger.LogWarning(
                "Placement race on job {JobId} for machine {MachineId} — trying next candidate",
                job.Id, best.Placement.Candidate.Machine.Id);

            // The failed save detached every tracked change, decision row included; re-read
            // the aggregate before mutating it again.
            var reloaded = await _jobs.GetByIdAsync(job.Id, cancellationToken);
            if (reloaded is null)
            {
                return await RecordFailureAsync(
                    job, request, filter, ranked, AssignmentOutcomes.PlacementRace,
                    "Job disappeared during assignment", stopwatch, budget, budgetExceeded, earliestEnd, cancellationToken);
            }

            job = reloaded;
        }

        _logger.LogWarning(
            "Job {JobId}: no candidate could be committed (trigger {Trigger}, budget exceeded: {BudgetExceeded})",
            job.Id, request.Trigger, budgetExceeded);

        return await RecordFailureAsync(
            job, request, filter, ranked, AssignmentOutcomes.PlacementRace,
            "Every candidate placement collided with a concurrent booking — retry shortly",
            stopwatch, budget, budgetExceeded, earliestEnd, cancellationToken);
    }

    /// <summary>
    /// Drops the lab responsible for the triggering event (BR-RESCHED-001: a refusing lab must
    /// not be handed the same job back). Excluding a lab is a preference, not an absolute:
    /// BR-SCHED-006 wins, so a lab that is the only capable one stays in the running.
    /// </summary>
    private CapabilityFilterResult ApplyExclusion(
        CapabilityFilterResult filter,
        Guid? excludedLabId,
        Guid jobId)
    {
        if (excludedLabId is not { } excluded)
            return filter;

        var remaining = filter.Candidates.Where(c => c.Lab.Id != excluded).ToList();
        if (remaining.Count == 0)
        {
            _logger.LogWarning(
                "Job {JobId}: excluding lab {LabId} would leave no capable candidate — keeping it as the last resort",
                jobId, excluded);
            return filter;
        }

        return new CapabilityFilterResult(remaining, filter.Rejections);
    }

    /// <summary>
    /// Logs an outcome that placed nothing. The entry is still evidence of *why* the job is
    /// still unplaced (BR-ASSIGN-004), so it is committed on its own.
    /// </summary>
    private async Task<AssignmentOutcome> RecordFailureAsync(
        Job job,
        AssignmentRequest request,
        CapabilityFilterResult filter,
        IReadOnlyList<ScoredPlacement> ranked,
        string outcome,
        string reason,
        Stopwatch stopwatch,
        TimeSpan budget,
        bool budgetExceeded,
        DateTime? earliestEnd,
        CancellationToken cancellationToken)
    {
        await _decisions.AddAsync(
            BuildDecision(job, request, outcome, filter, ranked, stopwatch, budget, budgetExceeded, chosen: null, reason: reason),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AssignmentOutcome(
            false, null, outcome, reason, ranked, filter.Rejections,
            Elapsed(stopwatch), budgetExceeded, earliestEnd);
    }

    private AssignmentDecision BuildDecision(
        Job job,
        AssignmentRequest request,
        string outcome,
        CapabilityFilterResult filter,
        IReadOnlyList<ScoredPlacement> ranked,
        Stopwatch stopwatch,
        TimeSpan budget,
        bool budgetExceeded,
        ScoredPlacement? chosen,
        string? reason = null)
    {
        return AssignmentDecision.Record(
            job,
            trigger: request.Trigger,
            outcome: outcome,
            candidatesJson: DecisionSnapshotJson.Candidates(filter, request.ExcludeLabId),
            rankingJson: DecisionSnapshotJson.Ranking(ranked),
            scoringConfigVersion: _scoringConfig.Version,
            scoringConfigJson: DecisionSnapshotJson.ScoringConfig(_scoringConfig),
            timeBudgetMs: (int)budget.TotalMilliseconds,
            elapsedMs: Elapsed(stopwatch),
            budgetExceeded: budgetExceeded,
            reason: reason,
            actorType: request.ActorType,
            actorId: request.ActorId,
            chosenLabId: chosen?.Placement.Candidate.Lab.Id,
            chosenMachineId: chosen?.Placement.Candidate.Machine.Id,
            chosenScore: chosen?.Score);
    }

    private static int Elapsed(Stopwatch stopwatch) => (int)stopwatch.ElapsedMilliseconds;
}
