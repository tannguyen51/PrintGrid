using MediatR;
using PrintGrid.Modules.Customer.Domain.Events;
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
/// internal due dates (BR-SCHED-001). Placement/anti-overlap happens in the assign step;
/// this handler stays deterministic and transactional with OrderConfirmedEvent.
/// </summary>
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    /// <summary>Days the hub needs (receipt + QC + consolidation) before the customer date (cfg in FR-ADMIN-002 later).</summary>
    public const int HubAndTransitBufferDays = 2;

    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;

    public OrderConfirmedEventHandler(IJobRepository jobs, IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
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
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
