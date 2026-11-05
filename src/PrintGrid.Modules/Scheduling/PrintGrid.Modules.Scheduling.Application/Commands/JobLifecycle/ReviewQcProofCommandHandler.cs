using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public sealed class ReviewQcProofCommandHandler(IJobRepository jobs, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    : IRequestHandler<ReviewQcProofCommand, Result>
{
    public async Task<Result> Handle(ReviewQcProofCommand command, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        var result = job.ReviewQcProof(command.Approved, command.StaffId, command.Reason, clock.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
