using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

using PrintGrid.Modules.Scheduling.Domain.Enums;

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
        if (command.RequestingUserId.HasValue)
        {
            if (command.IsAdminOrOps)
                lab.ClaimOwnership(command.RequestingUserId.Value);
            else if (lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
                return Result.Failure<MachineDto>(Error.Forbidden("Bạn chỉ có quyền thêm máy vào xưởng của mình."));
        }
        else if (command.ActorUserId.HasValue && lab.OwnerUserId != command.ActorUserId)
        {
            return Result.Failure<MachineDto>(Error.Forbidden("You can only manage machines in your own lab"));
        }
        if (lab.Status is LabStatus.Inactive or LabStatus.Rejected || (!lab.IsActive && lab.Status != LabStatus.Pending))
            return Result.Failure<MachineDto>(Error.Conflict($"Lab '{lab.Name}' is deactivated and cannot accept machines"));

        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Model))
            return Result.Failure<MachineDto>(Error.Validation("Tên máy và model không được để trống."));

        if (command.BuildWidthMm <= 0 || command.BuildDepthMm <= 0 || command.BuildHeightMm <= 0)
            return Result.Failure<MachineDto>(Error.Validation("Khổ in (X, Y, Z) phải lớn hơn 0."));

        if (command.MinLayerHeightMm <= 0 || command.AchievableToleranceMm <= 0)
            return Result.Failure<MachineDto>(Error.Validation("Layer height và dung sai phải lớn hơn 0."));

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
        _labs.AddMachine(machine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LabMappings.ToDto(machine));
    }
}
