using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

/// <summary>
/// Bridges Scheduling domain events to the Customer-facing production status
/// (contracts/events.md). An item is only Complete when EVERY job for that order item is
/// Completed — a split order item (BR-SCHED-010) stays in production until all of its child
/// batches have passed hub inspection.
/// </summary>
public class JobStatusIntegrationPublisher :
    INotificationHandler<JobStartedEvent>,
    INotificationHandler<JobCompletedEvent>,
    INotificationHandler<JobFailedEvent>,
    INotificationHandler<JobInspectionPassedEvent>
{
    private readonly IPublisher _publisher;
    private readonly IJobRepository _jobs;
    private readonly ILogger<JobStatusIntegrationPublisher> _logger;

    public JobStatusIntegrationPublisher(
        IPublisher publisher,
        IJobRepository jobs,
        ILogger<JobStatusIntegrationPublisher> logger)
    {
        _publisher = publisher;
        _jobs = jobs;
        _logger = logger;
    }

    public Task Handle(JobStartedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "Printing"),
            cancellationToken);
    }

    public Task Handle(JobCompletedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "QualityCheck"),
            cancellationToken);
    }

    public Task Handle(JobFailedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "Failed", notification.Reason),
            cancellationToken);
    }

    /// <summary>
    /// Hub inspection passed (FR-HUB-002): the job itself is Completed, but the order item is
    /// only ready when every live sibling job for the same item is Completed, or Failed and
    /// superseded by a later reprint (FR-HUB-003). The event carries only the JobId, so the
    /// aggregate is reloaded to resolve its OrderItemId.
    /// </summary>
    public async Task Handle(JobInspectionPassedEvent notification, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(notification.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning(
                "Inspection-passed event for unknown job {JobId}; no production status published",
                notification.JobId);
            return;
        }

        var siblings = await _jobs.GetByOrderItemIdAsync(job.OrderItemId, cancellationToken);

        // A cancelled job can never become Completed: it is the superseded original of a split
        // (BR-SCHED-010) and counting it would keep the item open forever.
        var liveJobs = siblings.Where(candidate => candidate.Status != JobStatus.Cancelled).ToList();

        // A failed job blocks the item only until a later reprint of the same chain exists
        // (FR-HUB-003). The failed original keeps the same OrderItemId, so without this an
        // item could never complete after a reprint passed.
        bool Superseded(Guid id, Guid? originalJobId, int reprintIndex) =>
            liveJobs.Any(other =>
                other.Id != id &&
                other.ReprintIndex > reprintIndex &&
                (other.OriginalJobId ?? other.Id) == (originalJobId ?? id));

        var openJobs = liveJobs
            .Where(candidate => candidate.Status is not (JobStatus.Completed or JobStatus.Failed))
            .Select(candidate => $"{candidate.Id}:{candidate.Status}")
            .ToList();
        var unresolvedFailures = liveJobs
            .Where(candidate => candidate.Status == JobStatus.Failed &&
                                !Superseded(candidate.Id, candidate.OriginalJobId, candidate.ReprintIndex))
            .Select(candidate => $"{candidate.Id}:Failed")
            .ToList();
        var anyCompleted = liveJobs.Any(candidate => candidate.Status == JobStatus.Completed);

        if (!anyCompleted || openJobs.Count > 0 || unresolvedFailures.Count > 0)
        {
            var blockers = openJobs.Concat(unresolvedFailures).ToList();
            _logger.LogInformation(
                "Order item {OrderItemId} is not complete yet after job {JobId} passed inspection: {OpenCount}/{TotalCount} job(s) still open ({OpenJobs})",
                job.OrderItemId, job.Id, blockers.Count, liveJobs.Count, string.Join(", ", blockers));
            return;
        }

        await _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(job.OrderItemId, "Completed"),
            cancellationToken);
    }
}
