using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class CompleteJobCommandHandler : IRequestHandler<CompleteJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CompleteJobCommandHandler(IJobRepository jobs, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(CompleteJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var result = job.Complete(_clock.UtcNow, command.ActualPrintMinutes);
        if (result.IsFailure) return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}