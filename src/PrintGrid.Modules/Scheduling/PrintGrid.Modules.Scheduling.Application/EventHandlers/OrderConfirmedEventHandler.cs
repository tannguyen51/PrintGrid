using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

/// <summary>
/// Decomposes a confirmed order into pending jobs using frozen quote estimates.
/// Ops initiates the first machine assignment from the scheduling board.
/// </summary>
public class OrderConfirmedEventHandler(
    IJobRepository jobs,
    IUnitOfWork unitOfWork,
    ILogger<OrderConfirmedEventHandler> logger)
    : INotificationHandler<OrderConfirmedEvent>
{
    /// <summary>Days reserved for transit and processing at the hub.</summary>
    public const int HubAndTransitBufferDays = 2;

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items ?? [])
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
                item.OrderItemId, item.ModelId, spec, totalMinutes, internalDue, item.Quantity);
            job.SetQuotedProduction(item.CalculationJson, item.QuotedMachineId);
            await jobs.AddAsync(job, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Order {OrderId} created pending production jobs awaiting Ops assignment", notification.OrderId);
    }
}
