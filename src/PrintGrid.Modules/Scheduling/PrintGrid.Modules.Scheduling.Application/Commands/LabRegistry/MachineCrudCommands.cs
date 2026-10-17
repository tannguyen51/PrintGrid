using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record UpdateMachineCommand(
    Guid LabId,
    Guid MachineId,
    string Name,
    string Model,
    PrintTechnology Technology,
    decimal BuildWidthMm,
    decimal BuildDepthMm,
    decimal BuildHeightMm,
    decimal MinLayerHeightMm,
    decimal AchievableToleranceMm,
    IReadOnlyList<string> SupportedMaterials,
    Guid? RequestingUserId = null,
    bool IsAdminOrOps = true) : IRequest<Result<MachineDto>>;

public record DeleteMachineCommand(
    Guid LabId,
    Guid MachineId,
    Guid? RequestingUserId = null,
    bool IsAdminOrOps = true) : IRequest<Result>;

public record ScheduleMachineMaintenanceCommand(
    Guid LabId,
    Guid MachineId,
    DateTime? MaintenanceFromUtc,
    DateTime? MaintenanceUntilUtc,
    string? Reason,
    bool Clear = false,
    bool SetMaintenanceStatusNow = false,
    Guid? RequestingUserId = null,
    bool IsAdminOrOps = true) : IRequest<Result<MachineDto>>;

public class MachineCrudCommandHandler :
    IRequestHandler<UpdateMachineCommand, Result<MachineDto>>,
    IRequestHandler<DeleteMachineCommand, Result>,
    IRequestHandler<ScheduleMachineMaintenanceCommand, Result<MachineDto>>
{
    private readonly ILabRepository _labs;
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;

    public MachineCrudCommandHandler(ILabRepository labs, IJobRepository jobs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _jobs = jobs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MachineDto>> Handle(UpdateMachineCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<MachineDto>(Error.NotFound("Lab", command.LabId));

        if (!command.IsAdminOrOps && command.RequestingUserId.HasValue)
        {
            if (lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
                return Result.Failure<MachineDto>(Error.Forbidden("Bạn chỉ có quyền chỉnh sửa máy thuộc xưởng của mình."));
        }

        var machine = lab.Machines.SingleOrDefault(m => m.Id == command.MachineId);
        if (machine is null)
            return Result.Failure<MachineDto>(Error.NotFound("Machine", command.MachineId));

        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Model))
            return Result.Failure<MachineDto>(Error.Validation("Tên máy và model không được để trống."));

        if (command.BuildWidthMm <= 0 || command.BuildDepthMm <= 0 || command.BuildHeightMm <= 0)
            return Result.Failure<MachineDto>(Error.Validation("Khổ in (X, Y, Z) phải lớn hơn 0."));

        if (command.MinLayerHeightMm <= 0 || command.AchievableToleranceMm <= 0)
            return Result.Failure<MachineDto>(Error.Validation("Layer height và dung sai phải lớn hơn 0."));

        var buildVolume = BuildVolume.Create(command.BuildWidthMm, command.BuildDepthMm, command.BuildHeightMm);
        machine.UpdateSpecifications(
            command.Name,
            command.Model,
            command.Technology,
            buildVolume,
            command.MinLayerHeightMm,
            command.AchievableToleranceMm,
            command.SupportedMaterials);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LabMappings.ToDto(machine));
    }

    public async Task<Result> Handle(DeleteMachineCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure(Error.NotFound("Lab", command.LabId));

        if (!command.IsAdminOrOps && command.RequestingUserId.HasValue)
        {
            if (lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
                return Result.Failure(Error.Forbidden("Bạn chỉ có quyền xóa máy thuộc xưởng của mình."));
        }

        var machine = lab.Machines.SingleOrDefault(m => m.Id == command.MachineId);
        if (machine is null)
            return Result.Failure(Error.NotFound("Machine", command.MachineId));

        var activeJobs = await _jobs.GetActiveByMachineAsync(command.MachineId, cancellationToken);
        if (activeJobs.Count > 0)
        {
            return Result.Failure(Error.Conflict(
                $"Không thể xóa máy '{machine.Name}' vì đang có {activeJobs.Count} đơn in chưa hoàn tất trên máy này."));
        }

        lab.RemoveMachine(machine.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<MachineDto>> Handle(ScheduleMachineMaintenanceCommand command, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(command.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<MachineDto>(Error.NotFound("Lab", command.LabId));

        if (!command.IsAdminOrOps && command.RequestingUserId.HasValue)
        {
            if (lab.OwnerUserId.HasValue && lab.OwnerUserId.Value != command.RequestingUserId.Value)
                return Result.Failure<MachineDto>(Error.Forbidden("Bạn chỉ có quyền đặt lịch bảo trì máy thuộc xưởng của mình."));
        }

        var machine = lab.Machines.SingleOrDefault(m => m.Id == command.MachineId);
        if (machine is null)
            return Result.Failure<MachineDto>(Error.NotFound("Machine", command.MachineId));

        if (command.Clear)
        {
            machine.ClearMaintenance();
        }
        else
        {
            if (command.MaintenanceFromUtc.HasValue
                && command.MaintenanceUntilUtc.HasValue
                && command.MaintenanceUntilUtc.Value <= command.MaintenanceFromUtc.Value)
            {
                return Result.Failure<MachineDto>(Error.Validation("Thời gian kết thúc bảo trì phải sau thời gian bắt đầu."));
            }

            machine.ScheduleMaintenance(
                command.MaintenanceFromUtc,
                command.MaintenanceUntilUtc,
                command.Reason,
                command.SetMaintenanceStatusNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LabMappings.ToDto(machine));
    }
}

