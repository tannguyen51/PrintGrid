using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetNetworkStatsQuery : IRequest<Result<NetworkStatsDto>>;

public record NetworkStatsDto(
    int TotalLabs,
    int ActiveLabs,
    int TotalMachines,
    int FdmMachines,
    int SlaMachines,
    int SlsMachines,
    decimal AverageOnTimeDeliveryRate,
    decimal AverageFirstPassYield,
    IReadOnlyList<NetworkRegionDto> Regions);

/// <summary>City-level aggregate — deliberately no lab id, name or address.</summary>
public record NetworkRegionDto(string City, int Labs, int Machines);