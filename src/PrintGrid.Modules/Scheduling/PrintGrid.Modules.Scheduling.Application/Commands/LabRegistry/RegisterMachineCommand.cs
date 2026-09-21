using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record RegisterMachineCommand(
    Guid LabId,
    string Name,
    string Model,
    PrintTechnology Technology,
    decimal BuildWidthMm,
    decimal BuildDepthMm,
    decimal BuildHeightMm,
    decimal MinLayerHeightMm,
    decimal AchievableToleranceMm,
    IReadOnlyList<string> SupportedMaterials) : IRequest<Result<MachineDto>>;