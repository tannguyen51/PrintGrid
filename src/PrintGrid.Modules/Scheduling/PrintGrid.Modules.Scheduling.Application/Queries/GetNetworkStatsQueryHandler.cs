using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetNetworkStatsQueryHandler : IRequestHandler<GetNetworkStatsQuery, Result<NetworkStatsDto>>
{
    private readonly ILabRepository _labs;

    public GetNetworkStatsQueryHandler(ILabRepository labs) => _labs = labs;

    public async Task<Result<NetworkStatsDto>> Handle(
        GetNetworkStatsQuery query,
        CancellationToken cancellationToken)
    {
        var snapshot = await _labs.GetNetworkStatsAsync(cancellationToken);

        return Result.Success(new NetworkStatsDto(
            snapshot.TotalLabs,
            snapshot.ActiveLabs,
            snapshot.TotalMachines,
            snapshot.FdmMachines,
            snapshot.SlaMachines,
            snapshot.SlsMachines,
            decimal.Round(snapshot.AverageOnTimeDeliveryRate * 100m, 1),
            decimal.Round(snapshot.AverageFirstPassYield * 100m, 1)));
    }
}