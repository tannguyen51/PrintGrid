using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.DTOs;

public record MachineDto(
    Guid Id,
    string Name,
    string Model,
    PrintTechnology Technology,
    decimal BuildWidthMm,
    decimal BuildDepthMm,
    decimal BuildHeightMm,
    decimal MinLayerHeightMm,
    decimal AchievableToleranceMm,
    decimal SpeedFactor,
    MachineStatus Status,
    IReadOnlyCollection<string> SupportedMaterials);

public record LabDto(
    Guid Id,
    string Name,
    string City,
    bool IsActive,
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    int TransitDaysToHub,
    DateTime CreatedAt,
    IReadOnlyCollection<MachineDto> Machines);