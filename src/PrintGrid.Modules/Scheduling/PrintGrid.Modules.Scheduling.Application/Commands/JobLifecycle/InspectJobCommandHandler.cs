using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

/// <summary>
/// Hub QC decision (FR-HUB-002): PASS moves the job to Completed; FAIL triggers a
/// reprint (job fails and is prepared for reassignment to another machine).
/// </summary>
public class InspectJobCommandHandler : IRequestHandler<InspectJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;

    public InspectJobCommandHandler(IJobRepository jobs, IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(InspectJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        Result result;
        if (command.Passed)
        {
            if (job.Status != Domain.Enums.JobStatus.AwaitingInspection)
                return Result.Failure(Error.Conflict($"Job in state {job.Status} cannot be inspected"));
            job.MarkInspectionPassed();
            result = Result.Success();
        }
        else
        {
            job.Fail(command.Note ?? "Failed hub inspection");
            job.PrepareForReassignment(); // back to Pending for reprint
            result = Result.Success();
        }

        if (result.IsFailure) return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}