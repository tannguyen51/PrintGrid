using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class RegisterMachineCommandHandler : IRequestHandler<RegisterMachineCommand, Result<MachineDto>>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterMachineCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MachineDto>> Handle(RegisterMachineCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<MachineDto>(Error.NotFound("Lab", command.LabId));
        if (command.ActorUserId.HasValue && lab.OwnerUserId != command.ActorUserId)
            return Result.Failure<MachineDto>(Error.Forbidden("You can only manage machines in your own lab"));

        var buildVolume = BuildVolume.Create(
            command.BuildWidthMm, command.BuildDepthMm, command.BuildHeightMm);

        var machine = Machine.Register(
            lab.Id,
            command.Name,
            command.Model,
            command.Technology,
            buildVolume,
            command.MinLayerHeightMm,
            command.AchievableToleranceMm,
            command.SupportedMaterials);

        lab.AddMachine(machine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LabMappings.ToDto(machine));
    }
}
