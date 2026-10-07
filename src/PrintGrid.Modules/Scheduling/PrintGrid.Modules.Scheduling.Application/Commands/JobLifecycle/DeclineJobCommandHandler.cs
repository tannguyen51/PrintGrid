using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class DeclineJobCommandHandler : IRequestHandler<DeclineJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILabRepository _labs;

    public DeclineJobCommandHandler(IJobRepository jobs, ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeclineJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var labId = job.LabId;
        var result = job.Decline(command.Reason);
        if (result.IsFailure) return result;
        if (labId.HasValue)
        {
            var lab = await _labs.GetByIdAsync(labId.Value, cancellationToken);
            lab?.ReleaseMaterial(job.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
