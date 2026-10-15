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
        if (command.RequestingUserId.HasValue && lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
            return Result.Failure(Error.Forbidden("Bạn chỉ có quyền quản lý máy trong xưởng của mình."));
        if (command.ActorUserId.HasValue && lab.OwnerUserId != command.ActorUserId)
            return Result.Failure(Error.Forbidden("You can only manage machines in your own lab"));

        if (!command.IsAdminOrOps && command.RequestingUserId.HasValue)
        {
            if (lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
                return Result.Failure(Error.Forbidden("Bạn chỉ có quyền đổi trạng thái máy thuộc xưởng của mình."));
        }

        var machine = lab.Machines.FirstOrDefault(m => m.Id == command.MachineId);
        if (machine is null)
            return Result.Failure(Error.NotFound("Machine", command.MachineId));

        machine.SetStatus(command.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
