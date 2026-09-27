using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Application.Mappings;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, Result<IReadOnlyList<JobDto>>>
{
    private readonly IJobRepository _jobs;

    public GetJobsQueryHandler(IJobRepository jobs) => _jobs = jobs;

    public async Task<Result<IReadOnlyList<JobDto>>> Handle(GetJobsQuery query, CancellationToken cancellationToken)
    {
        var entities = await _jobs.GetByStatusAsync(query.Status, cancellationToken);
        var dtos = entities
            .OrderBy(j => j.InternalDueDate)
            .Select(JobMappings.ToDto)
            .ToList();
        return Result.Success<IReadOnlyList<JobDto>>(dtos);
    }
}