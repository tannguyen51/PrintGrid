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
public class PlacementShortfallService : IPlacementShortfallService
{
    /// <summary>Kept in step with OrderConfirmedEventHandler.HubAndTransitBufferDays.</summary>
    public const int HubAndTransitBufferDays = 2;

    private readonly IAssignmentEngine _engine;
    private readonly IDateChangeRequestRepository _dateChanges;
    private readonly IOpsEscalationRepository _escalations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PlacementShortfallService> _logger;

    public PlacementShortfallService(
        IAssignmentEngine engine,
        IDateChangeRequestRepository dateChanges,
        IOpsEscalationRepository escalations,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IDateTimeProvider clock,
        ILogger<PlacementShortfallService> logger)
    {
        _engine = engine;
        _dateChanges = dateChanges;
        _escalations = escalations;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(Job job, string reason, CancellationToken cancellationToken = default)
    {
        // Ask the engine what the network could actually do if the date moved — a probe only
        // reads, it never places work past a committed date.
        var probe = await _engine.AssignAsync(
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

        var request = DateChangeRequest.Propose(
            job.Id,
            job.OrderItemId,
            originalDelivery,
            proposedDelivery,
            reason,
            // Token the customer-facing approval link carries (POST /date-change-requests/{token}/approve).
            token: Guid.NewGuid().ToString("N"),
            createdAtUtc: _clock.UtcNow);

        await _dateChanges.AddAsync(request, cancellationToken);
        await _escalations.AddAsync(
            OpsEscalation.Raise(job.Id, job.OrderItemId, OpsEscalationKind.NoFeasibleSlot, reason, _clock.UtcNow),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // BR-SCHED-008: the customer is asked BEFORE the date moves, never told afterwards.
        await _publisher.Publish(new OrderDelayedIntegrationEvent(job.OrderItemId, reason), cancellationToken);

        _logger.LogWarning(
            "Job {JobId} cannot hold its committed date ({Original:yyyy-MM-dd}); proposed {Proposed:yyyy-MM-dd} to the customer and escalated to ops (request {RequestId})",
            job.Id, originalDelivery, proposedDelivery, request.Id);
    }
}
