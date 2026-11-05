using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class DeclineJobCommandHandler(IJobRepository jobs, ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<DeclineJobCommand, Result>
{
    public async Task<Result> Handle(DeclineJobCommand command, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var labId = job.LabId;
        var result = job.Decline(command.Reason);
        if (result.IsFailure) return result;
        if (labId.HasValue)
        {
            var lab = await labs.GetByIdAsync(labId.Value, cancellationToken);
            lab?.ReleaseMaterial(job.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
