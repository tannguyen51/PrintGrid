using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, Result<IReadOnlyList<JobDto>>>
{
    private readonly IJobRepository _jobs;
    private readonly IOrderRepository _orders;
    private readonly IModelRepository _models;
    private readonly SharedKernel.Interfaces.IUnitOfWork _unitOfWork;

    public GetJobsQueryHandler(
        IJobRepository jobs,
        IOrderRepository orders,
        IModelRepository models,
        PrintGrid.SharedKernel.Interfaces.IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _orders = orders;
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<JobDto>>> Handle(GetJobsQuery query, CancellationToken cancellationToken)
    {
        var entities = await _jobs.GetByStatusAsync(query.Status, cancellationToken);
        var now = DateTime.UtcNow;

        if (query.Status == JobStatus.Assigned)
        {
            var timedOut = false;
            var activeAssigned = new List<Domain.Entities.Job>();

            foreach (var job in entities)
            {
                if (job.AssignedAtUtc.HasValue && (now - job.AssignedAtUtc.Value) > TimeSpan.FromHours(2))
                {
                    job.TimeoutAcceptance(now);
                    timedOut = true;
                }
                else
                {
                    activeAssigned.Add(job);
                }
            }

            if (timedOut)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                entities = activeAssigned;
            }
        }

        var ordered = entities.OrderBy(j => j.InternalDueDate).ToList();

        // Order/model context: one lookup per distinct id (status lists are small, tens of jobs).
        // Lets ops see "PG-20261008-00001 · bracket.stl" and the lab see the file it must print,
        // instead of bare GUIDs. Only order number + file name + hash cross this line — no PII.
        var orders = new Dictionary<Guid, PrintGrid.Modules.Customer.Domain.Entities.Order>();
        foreach (var itemId in ordered.Select(j => j.OrderItemId).Distinct())
        {
            var order = await _orders.GetByItemIdAsync(itemId, cancellationToken);
            if (order is not null) orders[itemId] = order;
        }

        var models = new Dictionary<Guid, PrintGrid.Modules.Customer.Domain.Entities.Model>();
        foreach (var modelId in ordered.Select(j => j.ModelId).Distinct())
        {
            var model = await _models.GetByIdAsync(modelId, cancellationToken);
            if (model is not null) models[modelId] = model;
        }

        var dtos = ordered
            .Select(job =>
            {
                var dto = JobMappings.ToDto(job);
                var order = orders.GetValueOrDefault(job.OrderItemId);
                var model = models.GetValueOrDefault(job.ModelId);
                return dto with
                {
                    OrderNumber = order?.OrderNumber,
                    ModelFileName = model?.FileName,
                    Sha256 = model?.Sha256,
                };
            })
            .ToList();

        return Result.Success<IReadOnlyList<JobDto>>(dtos);
    }
}