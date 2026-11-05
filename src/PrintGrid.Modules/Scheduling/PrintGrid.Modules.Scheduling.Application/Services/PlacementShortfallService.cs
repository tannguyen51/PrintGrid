using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.Services;

/// <summary>
/// Implements the "xin dời" branch (BR-SCHED-008 / BR-SCHED-011 / BR-NOTIFY-002): when the
/// committed delivery date cannot be met, the platform states the earliest date it CAN meet,
/// asks the customer to approve, and flags the case for operations — it is never late silently.
/// </summary>
public abstract class PlacementShortfallService(
    IAssignmentEngine engine,
    IDateChangeRequestRepository dateChanges,
    IOpsEscalationRepository escalations,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    IDateTimeProvider clock,
    ILogger<PlacementShortfallService> logger)
    : IPlacementShortfallService
{
    /// <summary>Kept in step with OrderConfirmedEventHandler.HubAndTransitBufferDays.</summary>
    public const int HubAndTransitBufferDays = 2;

    public async Task HandleAsync(Job job, string reason, CancellationToken cancellationToken = default)
    {
        // Ask the engine what the network could actually do if the date moved — a probe only
        // reads, it never places work past a committed date.
        var probe = await engine.AssignAsync(
            job,
            new AssignmentRequest(
                AssignmentTriggers.DateChangeProbe,
                TimeBudget: TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds),
                IgnoreDueDate: true,
                ProbeOnly: true),
            cancellationToken);

        var originalDelivery = job.InternalDueDate.AddDays(HubAndTransitBufferDays);
        var earliestEndUtc = probe.EarliestFeasibleEndUtc
                             ?? job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var proposedDelivery = DateOnly.FromDateTime(earliestEndUtc).AddDays(HubAndTransitBufferDays);
        if (proposedDelivery <= originalDelivery)
            proposedDelivery = originalDelivery.AddDays(1);

        if (job.AttemptNumber < 2)
        {
            await escalations.AddAsync(
                OpsEscalation.Raise(job.Id, job.OrderItemId, OpsEscalationKind.NoFeasibleSlot,
                    $"Date change blocked by B16: {reason}", clock.UtcNow), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Job {JobId} cannot hold its date, but B16 blocks a proposal before two reprints", job.Id);
            return;
        }

        var request = DateChangeRequest.Propose(
            job.Id,
            job.OrderItemId,
            originalDelivery,
            proposedDelivery,
            DateChangeReason.ReprintDeadlineUnrecoverable,
            reason,
            // Token the customer-facing approval link carries (POST /date-change-requests/{token}/approve).
            token: Guid.NewGuid().ToString("N"),
            createdAtUtc: clock.UtcNow);

        await dateChanges.AddAsync(request, cancellationToken);
        await escalations.AddAsync(
            OpsEscalation.Raise(job.Id, job.OrderItemId, OpsEscalationKind.NoFeasibleSlot, reason, clock.UtcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // BR-SCHED-008: the customer is asked BEFORE the date moves, never told afterward.
        await publisher.Publish(new OrderDelayedIntegrationEvent(job.OrderItemId), cancellationToken);

        logger.LogWarning(
            "Job {JobId} cannot hold its committed date ({Original:yyyy-MM-dd}); proposed {Proposed:yyyy-MM-dd} to the customer and escalated to ops (request {RequestId})",
            job.Id, originalDelivery, proposedDelivery, request.Id);
    }
}
