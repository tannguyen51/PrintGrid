using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed class UpdateMachineCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMachineCommand, Result<MachineDto>>
{
    public async Task<Result<MachineDto>> Handle(UpdateMachineCommand command, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null) return Result.Failure<MachineDto>(Error.NotFound("Lab", command.LabId));
        if (lab.OwnerUserId != command.ActorUserId)
            return Result.Failure<MachineDto>(Error.Forbidden("Only the lab owner can update its machines"));

        var machine = lab.Machines.SingleOrDefault(x => x.Id == command.MachineId);
        if (machine is null) return Result.Failure<MachineDto>(Error.NotFound("Machine", command.MachineId));

        try
        {
            machine.UpdateSpecifications(command.Name, command.Model, command.Technology,
                BuildVolume.Create(command.BuildWidthMm, command.BuildDepthMm, command.BuildHeightMm),
                command.MinLayerHeightMm, command.AchievableToleranceMm, command.SupportedMaterials);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<MachineDto>(Error.Validation(exception.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LabMappings.ToDto(machine));
    }
}
