using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public sealed class ReviewQcProofCommandHandler : IRequestHandler<ReviewQcProofCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ReviewQcProofCommandHandler(IJobRepository jobs, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ReviewQcProofCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var result = job.ReviewQcProof(command.Approved, command.StaffId, command.Reason, _clock.UtcNow);
        if (result.IsFailure) return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
