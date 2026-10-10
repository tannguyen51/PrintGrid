using MediatR;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;

/// <summary>
/// Manual assignment from the ops board (FR-SCHED-006 / UC-011). The work itself lives in
/// <see cref="IAssignmentEngine"/> so that automatic rescheduling (FR-SCHED-007) and urgent
/// reprints (FR-HUB-003) run exactly the same logic — and leave the same decision log entry.
/// </summary>
public class AssignJobCommandHandler : IRequestHandler<AssignJobCommand, Result<AssignmentResultDto>>
{
    private readonly IJobRepository _jobs;
    private readonly IAssignmentEngine _engine;

    public AssignJobCommandHandler(IJobRepository jobs, IAssignmentEngine engine)
    {
        _jobs = jobs;
        _engine = engine;
    }

    public async Task<Result<AssignmentResultDto>> Handle(
        AssignJobCommand command,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
            return Result.Failure<AssignmentResultDto>(Error.NotFound("Job", command.JobId));

        var outcome = await _engine.AssignAsync(
            job,
            new AssignmentRequest(AssignmentTriggers.InitialAssign, ActorType: "ops"),
            cancellationToken);

        return outcome.Assigned
            ? Result.Success(outcome.Result!)
            : Result.Failure<AssignmentResultDto>(Error.Conflict(outcome.Reason ?? "Assignment failed"));
    }
}
