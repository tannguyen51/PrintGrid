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
public class ReschedulingTriggeredEventHandler(
    IJobRepository jobs,
    IAssignmentEngine engine,
    IPlacementShortfallService shortfall,
    ILogger<ReschedulingTriggeredEventHandler> logger)
    : INotificationHandler<ReschedulingTriggeredEvent>
{
    public async Task Handle(ReschedulingTriggeredEvent notification, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(notification.JobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Reschedule requested for unknown job {JobId}", notification.JobId);
            return;
        }

        // BR-SCHED-007: work that is printing, awaiting inspection or already done is never
        // reshuffled. Only a job waiting for a placement can be replanned; a *failed* job is
        // handled by the reprint flow (FR-HUB-003), not by re-placing the dead job.
        if (job.Status is not (JobStatus.Pending or JobStatus.Reassigned))
        {
            logger.LogInformation(
                "Reschedule skipped for job {JobId} in state {Status} (trigger {Trigger}, BR-SCHED-007)",
                job.Id, job.Status, notification.Trigger);
            return;
        }

        var outcome = await engine.AssignAsync(
            job,
            new AssignmentRequest(
                notification.Trigger,
                TimeBudget: TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds),
                // BR-RESCHED-001: repair the plan around the party that caused the event.
                ExcludeLabId: notification.ExcludedLabId ?? job.LabId),
            cancellationToken);

        if (outcome.Assigned)
        {
            logger.LogInformation(
                "Job {JobId} replanned after {Trigger} in {ElapsedMs} ms (budget exceeded: {BudgetExceeded})",
                job.Id, notification.Trigger, outcome.ElapsedMs, outcome.BudgetExceeded);
            return;
        }

        // A lost placement race is not a shortfall: the job stays Pending and can be placed
        // again. Only a job the network genuinely cannot fit before its date goes to the
        // customer-facing date-change flow.
        if (outcome.Outcome is not (AssignmentOutcomes.NoFeasibleSlot or AssignmentOutcomes.NoCapableMachine))
        {
            logger.LogWarning(
                "Job {JobId} left unplaced after {Trigger} ({Outcome}) — retry from the ops board",
                job.Id, notification.Trigger, outcome.Outcome);
            return;
        }

        logger.LogWarning(
            "Job {JobId} cannot hold its committed date after {Trigger}: {Reason}. Handing over to the date-change flow (BR-SCHED-011)",
            job.Id, notification.Trigger, outcome.Reason);

        await shortfall.HandleAsync(job, outcome.Reason ?? "No feasible slot after reschedule", cancellationToken);
    }
}
