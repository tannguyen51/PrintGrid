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
/// Decomposes deposit-confirmed orders. Small orders enter the assignment engine;
/// large orders and failed automatic placements remain available to Ops.
/// </summary>
public class OrderConfirmedEventHandler(
    IJobRepository jobs,
    IUnitOfWork unitOfWork,
    ILogger<OrderConfirmedEventHandler> logger,
    PrintGrid.Modules.Scheduling.Application.Abstractions.IAssignmentEngine? engine = null,
    ILabRepository? labs = null,
    Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    : INotificationHandler<OrderConfirmedEvent>
{
    /// <summary>Days reserved for transit and processing at the hub.</summary>
    public const int HubAndTransitBufferDays = 2;

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var created = new List<Job>();
        var network = labs is null ? [] : await labs.GetActiveWithMachinesAsync(cancellationToken);
        var maxTransit = network.Select(l => l.TransitDaysToHub).DefaultIfEmpty(1).Max();
        var lastMile = int.TryParse(configuration?["Delivery:LastMileDays"], out var days) ? Math.Clamp(days, 1, 30) : 2;
        foreach (var item in notification.Items ?? [])
        {
            // Retrying an event must not duplicate the order item's production quantity.
            if ((await jobs.GetByOrderItemIdAsync(item.OrderItemId, cancellationToken)).Count > 0) continue;
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

            var transit = item.QuotedMachineId.HasValue
                ? network.FirstOrDefault(l => l.Machines.Any(m => m.Id == item.QuotedMachineId))?.TransitDaysToHub ?? maxTransit
                : maxTransit;
            var internalDue = notification.PromisedDeliveryDate.AddDays(-(transit + 1 + lastMile));
            var job = Job.Create(
                item.OrderItemId, item.ModelId, spec, totalMinutes, internalDue, item.Quantity,
                deliveryBufferDays: transit + 1 + lastMile, lastMileDays: lastMile);
            job.SetQuotedProduction(item.CalculationJson, item.QuotedMachineId);
            await jobs.AddAsync(job, cancellationToken);
            created.Add(job);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // This event is raised after the payment/deposit gate. Large orders remain with Ops.
        if (notification.AutoAssign && !OrderRoutingPolicy.RequiresManualDispatch((notification.Items ?? []).Sum(i => i.Quantity)) && engine is not null)
            foreach (var job in created)
                await engine.AssignAsync(job, new Abstractions.AssignmentRequest(Abstractions.AssignmentTriggers.InitialAssign), cancellationToken);
        logger.LogInformation(
            "Order {OrderId}: created {JobCount} jobs; automatic dispatch {AutoAssign}", notification.OrderId, created.Count, notification.AutoAssign);
    }
}
