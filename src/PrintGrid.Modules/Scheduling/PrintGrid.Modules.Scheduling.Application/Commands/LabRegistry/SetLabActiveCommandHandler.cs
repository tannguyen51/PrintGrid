using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class SetLabActiveCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<SetLabActiveCommand, Result>
{
    public async Task<Result> Handle(SetLabActiveCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure(Error.NotFound("Lab", command.LabId));

        switch (command.IsActive)
        {
            case true when lab.Machines.Count == 0:
                return Result.Failure(Error.Conflict("A lab must declare at least one machine before approval"));
            case true when lab.MaterialStocks.Count == 0:
                return Result.Failure(Error.Conflict("A lab must declare at least one material stock before approval"));
            case true when (lab.TransitDaysToHub <= 0 || string.IsNullOrWhiteSpace(lab.WorkingDays)):
                return Result.Failure(Error.Conflict("Working calendar and transfer time are required before approval"));
            case true:
                lab.Activate();
                break;
            default:
                lab.Deactivate();
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
