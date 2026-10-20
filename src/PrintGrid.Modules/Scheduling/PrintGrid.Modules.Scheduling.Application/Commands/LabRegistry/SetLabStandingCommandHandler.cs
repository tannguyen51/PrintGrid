using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed class SetLabStandingCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<SetLabStandingCommand, Result>
{
    public async Task<Result> Handle(SetLabStandingCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));
        if (!lab.IsActive) return Result.Failure(Error.Conflict("A pending lab cannot be suspended or restored"));

        if (command.IsInGoodStanding) lab.RestoreGoodStanding();
        else lab.SuspendFromAssignments();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
