using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

/// <summary>
/// FR-SCHED-007: production events repair the unstarted plan automatically.
///
/// MVP triggers (09-Functional-Requirements.md:875) are a lab rejection and a print failure;
/// machine breakdown / reprint insertion are Should-have and arrive through the same seam.
/// The repair is bounded by the reschedule time budget (BR-SCHED-005), keeps a feasible
/// fallback (BR-SCHED-006), never touches work that is printing (BR-SCHED-007) and never
/// delays a committed date silently (BR-SCHED-008/011).
/// </summary>
public class ReschedulingTriggeredEventHandler : INotificationHandler<ReschedulingTriggeredEvent>
{
    private readonly IJobRepository _jobs;
    private readonly IAssignmentEngine _engine;
    private readonly IPlacementShortfallService _shortfall;
    private readonly ILogger<ReschedulingTriggeredEventHandler> _logger;

    public ReschedulingTriggeredEventHandler(
        IJobRepository jobs,
        IAssignmentEngine engine,
        IPlacementShortfallService shortfall,
        ILogger<ReschedulingTriggeredEventHandler> logger)
    {
        _jobs = jobs;
        _engine = engine;
        _shortfall = shortfall;
        _logger = logger;
    }

    public async Task Handle(ReschedulingTriggeredEvent notification, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(notification.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Reschedule requested for unknown job {JobId}", notification.JobId);
            return;
        }

        // BR-SCHED-007: work that is printing, awaiting inspection or already done is never
        // reshuffled. Only a job waiting for a placement can be replanned; a *failed* job is
        // handled by the reprint flow (FR-HUB-003), not by re-placing the dead job.
        if (job.Status is not (JobStatus.Pending or JobStatus.Reassigned))
        {
            _logger.LogInformation(
                "Reschedule skipped for job {JobId} in state {Status} (trigger {Trigger}, BR-SCHED-007)",
                job.Id, job.Status, notification.Trigger);
            return;
        }

        var outcome = await _engine.AssignAsync(
            job,
            new AssignmentRequest(
                notification.Trigger,
                TimeBudget: TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds),
                // BR-RESCHED-001: repair the plan around the party that caused the event.
                ExcludeLabId: notification.ExcludedLabId),
            cancellationToken);

        if (outcome.Assigned)
        {
            _logger.LogInformation(
                "Job {JobId} replanned after {Trigger} in {ElapsedMs} ms (budget exceeded: {BudgetExceeded})",
                job.Id, notification.Trigger, outcome.ElapsedMs, outcome.BudgetExceeded);
            return;
        }

        // A lost placement race is not a shortfall: the job stays Pending and can be placed
        // again. Only a job the network genuinely cannot fit before its date goes to the
        // customer-facing date-change flow.
        if (outcome.Outcome is not (AssignmentOutcomes.NoFeasibleSlot or AssignmentOutcomes.NoCapableMachine))
        {
            _logger.LogWarning(
                "Job {JobId} left unplaced after {Trigger} ({Outcome}) — retry from the ops board",
                job.Id, notification.Trigger, outcome.Outcome);
            return;
        }

        _logger.LogWarning(
            "Job {JobId} cannot hold its committed date after {Trigger}: {Reason}. Handing over to the date-change flow (BR-SCHED-011)",
            job.Id, notification.Trigger, outcome.Reason);

        await _shortfall.HandleAsync(job, outcome.Reason ?? "No feasible slot after reschedule", cancellationToken);
    }
}
