using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class AcceptJobCommandHandler : IRequestHandler<AcceptJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptJobCommandHandler(IJobRepository jobs, IUnitOfWork unitOfWork)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AcceptJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var result = job.Accept();
        if (result.IsFailure) return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}