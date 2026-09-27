using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class SetMachineStatusCommandHandler : IRequestHandler<SetMachineStatusCommand, Result>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public SetMachineStatusCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetMachineStatusCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure(Error.NotFound("Lab", command.LabId));

        var machine = lab.Machines.FirstOrDefault(m => m.Id == command.MachineId);
        if (machine is null)
            return Result.Failure(Error.NotFound("Machine", command.MachineId));

        machine.SetStatus(command.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}