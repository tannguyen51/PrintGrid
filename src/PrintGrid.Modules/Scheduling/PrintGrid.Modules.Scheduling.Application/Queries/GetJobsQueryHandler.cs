using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, Result<IReadOnlyList<JobDto>>>
{
    private readonly IJobRepository _jobs;
    private readonly PrintGrid.SharedKernel.Interfaces.IUnitOfWork _unitOfWork;

    public GetJobsQueryHandler(IJobRepository jobs, PrintGrid.SharedKernel.Interfaces.IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
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

        var dtos = entities
            .OrderBy(j => j.InternalDueDate)
            .Select(JobMappings.ToDto)
            .ToList();
        return Result.Success<IReadOnlyList<JobDto>>(dtos);
    }
}