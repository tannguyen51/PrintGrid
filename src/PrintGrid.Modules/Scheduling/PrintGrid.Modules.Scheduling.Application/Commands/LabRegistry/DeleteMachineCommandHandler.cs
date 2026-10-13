using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed class DeleteMachineCommandHandler(
    ILabRepository labs,
    IJobRepository jobs,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteMachineCommand, Result>
{
    public async Task<Result> Handle(DeleteMachineCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure(Error.NotFound("Lab", command.LabId));
        if (lab.OwnerUserId != command.ActorUserId)
            return Result.Failure(Error.Forbidden("Only the lab owner can delete its machines"));
        if (lab.Machines.All(x => x.Id != command.MachineId))
            return Result.Failure(Error.NotFound("Machine", command.MachineId));
        if (await jobs.HasActiveForMachineAsync(command.MachineId, cancellationToken))
            return Result.Failure(Error.Conflict("A machine with active jobs cannot be deleted"));

        lab.RemoveMachine(command.MachineId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
