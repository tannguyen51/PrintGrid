using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetNetworkStatsQueryHandler(ILabRepository labs)
    : IRequestHandler<GetNetworkStatsQuery, Result<NetworkStatsDto>>
{
    public async Task<Result<NetworkStatsDto>> Handle(
        GetNetworkStatsQuery query,
        CancellationToken cancellationToken)
    {
        var snapshot = await labs.GetNetworkStatsAsync(cancellationToken);

        return Result.Success(new NetworkStatsDto(
            snapshot.TotalLabs,
            snapshot.ActiveLabs,
            snapshot.TotalMachines,
            snapshot.FdmMachines,
            snapshot.SlaMachines,
            snapshot.SlsMachines,
            decimal.Round(snapshot.AverageOnTimeDeliveryRate * 100m, 1),
            decimal.Round(snapshot.AverageFirstPassYield * 100m, 1),
            snapshot.Regions.Select(region =>
                new NetworkRegionDto(region.City, region.Labs, region.Machines)).ToList()));
    }
}
