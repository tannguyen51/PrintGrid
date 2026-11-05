using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

/// <summary>One job by id — used by the lab's file endpoint to resolve OrderItemId → model file.</summary>
public record GetJobQuery(Guid JobId) : IRequest<Result<JobDto>>;

public class GetJobQueryHandler(IJobRepository jobs) : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(request.JobId, cancellationToken);
        return job is null
            ? Result.Failure<JobDto>(Error.NotFound("Job", request.JobId))
            : Result.Success(JobMappings.ToDto(job));
    }
}
