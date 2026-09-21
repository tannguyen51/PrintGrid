using MediatR;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

/// <summary>
/// When a customer order is confirmed, each printable line item becomes a production
/// Job (FR-SCHED-006). Jobs start in Pending and are picked up by the assignment engine.
/// </summary>
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IJobRepository _jobs;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public OrderConfirmedEventHandler(
        IJobRepository jobs,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items)
        {
            var spec = JobSpecification.Create(
                BuildVolume.Create(120m, 120m, 120m), // demo envelope; real sizing from model analysis
                item.MaterialCode,
                item.ColorCode,
                item.LayerHeightMm,
                item.ToleranceMm,
                PrintTechnology.Fdm,
                materialGrams: Math.Max(item.Quantity * 30m, 10m));

            // Internal due date = promised delivery minus transit/buffer (2 days demo).
            var internalDue = notification.PromisedDeliveryDate.AddDays(-2);

            var job = Job.Create(
                item.OrderItemId,
                item.ModelId,
                spec,
                estimatedPrintMinutes: Math.Max(item.Quantity * 60, 30),
                internalDue);

            await _jobs.AddAsync(job, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}