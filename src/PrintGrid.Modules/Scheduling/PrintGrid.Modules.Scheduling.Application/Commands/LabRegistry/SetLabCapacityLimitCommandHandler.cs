using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed class SetLabCapacityLimitCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<SetLabCapacityLimitCommand, Result>
{
    public async Task<Result> Handle(SetLabCapacityLimitCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));
        if (command.MaxConcurrentJobs is <= 0)
            return Result.Failure(Error.Validation("Capacity limit must be a positive number or empty"));

        lab.SetCapacityLimit(command.MaxConcurrentJobs);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
