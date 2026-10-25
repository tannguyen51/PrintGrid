using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

/// <summary>
/// FR-SCHED-006 (real numbers, replacing the T1 demo constants per contracts/events.md):
/// decomposes a paid Order into one Job per item using the FROZEN quote estimates —
/// per-unit minutes × quantity, grams × quantity, real bounding box — with backward
/// internal due dates (BR-SCHED-001).
///
/// The jobs are committed first, then placed immediately through the shared assign engine
/// with the <see cref="AssignmentTriggers.InitialAssign"/> trigger (the business decision
/// "auto-assign right after the deposit"). A job the network cannot place before its date is
/// never left late silently: <see cref="IPlacementShortfallService"/> escalates it and offers
/// the customer a new date (BR-SCHED-008/011). Each job is handled independently, so one
/// failure cannot stop the rest of the order from being placed.
/// </summary>
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    /// <summary>Days the hub needs (receipt + QC + consolidation) before the customer date (cfg in FR-ADMIN-002 later).</summary>
    public const int HubAndTransitBufferDays = 2;

    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssignmentEngine _engine;
    private readonly IPlacementShortfallService _shortfall;
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(
        IJobRepository jobs,
        IUnitOfWork unitOfWork,
        IAssignmentEngine engine,
        IPlacementShortfallService shortfall,
        ILogger<OrderConfirmedEventHandler> logger)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _engine = engine;
        _shortfall = shortfall;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var created = new List<Job>();

        foreach (var item in notification.Items)
        {
            var totalMinutes = Math.Max(item.EstimatedPrintMinutes * item.Quantity, 1);
            var totalGrams = Math.Max(item.EstimatedMaterialGrams * item.Quantity, 0.1m);

            var spec = JobSpecification.Create(
                BuildVolume.Create(
                    item.BoundingWidthMm ?? 120m,
                    item.BoundingDepthMm ?? 120m,
                    item.BoundingHeightMm ?? 120m),
                item.MaterialCode,
                item.ColorCode,
                item.LayerHeightMm,
                item.ToleranceMm,
                item.MaterialCode.Equals("RESIN", StringComparison.OrdinalIgnoreCase)
                    ? PrintTechnology.Sla
                    : PrintTechnology.Fdm,
                totalGrams);

            var internalDue = notification.PromisedDeliveryDate.AddDays(-HubAndTransitBufferDays);

            var job = Job.Create(
                item.OrderItemId,
                item.ModelId,
                spec,
                totalMinutes,
                internalDue, item.Quantity);

            await _jobs.AddAsync(job, cancellationToken);
            created.Add(job);
        }

        // Commit the jobs before placing them: the engine queries the network and writes its
        // own decision-log rows, so the job rows must already exist.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var job in created)
        {
            // Idempotency guard for a re-delivered OrderConfirmedEvent: a job that already
            // holds a lab has been placed and must not be run through the engine again.
            if (job.LabId.HasValue)
            {
                _logger.LogInformation(
                    "Auto-assign skipped for job {JobId}: already placed at lab {LabId}", job.Id, job.LabId);
                continue;
            }

            try
            {
                var outcome = await _engine.AssignAsync(
                    job,
                    new AssignmentRequest(
                        AssignmentTriggers.InitialAssign,
                        TimeBudget: TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds)),
                    cancellationToken);

                if (outcome.Assigned)
                {
                    _logger.LogInformation(
                        "Job {JobId} auto-assigned after order confirmation to {LabId}/{MachineId} (start {Start:u}, score {Score:F3})",
                        job.Id, outcome.Result!.LabId, outcome.Result.MachineId,
                        outcome.Result.PlannedStartUtc, outcome.Result.Score);
                    continue;
                }

                // A lost placement race leaves the job Pending and is retried from the ops board.
                // A real shortfall is escalated and renegotiated — never delayed silently.
                if (outcome.Outcome is AssignmentOutcomes.NoFeasibleSlot or AssignmentOutcomes.NoCapableMachine)
                {
                    _logger.LogWarning(
                        "Job {JobId} cannot be placed after order confirmation ({Outcome}): {Reason}. Handing over to the date-change flow (BR-SCHED-011)",
                        job.Id, outcome.Outcome, outcome.Reason);

                    await _shortfall.HandleAsync(
                        job,
                        outcome.Reason ?? "No feasible slot after order confirmation",
                        cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Job {JobId} left unplaced after order confirmation ({Outcome}) — retry from the ops board",
                    job.Id, outcome.Outcome);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One job must never block the rest of the order.
                _logger.LogError(
                    ex,
                    "Auto-assign failed for job {JobId}; the remaining jobs of the order continue",
                    job.Id);
            }
        }
    }
}
