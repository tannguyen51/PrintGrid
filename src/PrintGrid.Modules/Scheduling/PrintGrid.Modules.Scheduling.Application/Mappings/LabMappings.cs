using PrintGrid.Modules.Scheduling.Application.DTOs;

namespace PrintGrid.Modules.Scheduling.Application.Mappings;

public static class LabMappings
{
    public static MachineDto ToDto(Domain.Entities.Machine machine) => new(
        machine.Id,
        machine.Name,
        machine.Model,
        machine.Technology,
        machine.BuildVolume.WidthMm,
        machine.BuildVolume.DepthMm,
        machine.BuildVolume.HeightMm,
        machine.MinLayerHeightMm,
        machine.AchievableToleranceMm,
        machine.SpeedFactor,
        machine.Status,
        machine.SupportedMaterials.ToList(),
        machine.MaintenanceFromUtc,
        machine.MaintenanceUntilUtc,
        machine.MaintenanceReason);

    public static LabDto ToDto(Domain.Entities.Lab lab) => new(
        lab.Id,
        lab.Name,
        lab.City,
        lab.IsActive,
        lab.OnTimeDeliveryRate,
        lab.FirstPassYield,
        lab.TransitDaysToHub,
        lab.CreatedAt,
        lab.Machines.Select(ToDto).ToList(),
        lab.Status.ToString(),
        lab.OperatingHours,
        lab.OwnerUserId,
        lab.ContactPhone,
        lab.Address,
        lab.RejectionReason,
        lab.IsDeclarationComplete);
}