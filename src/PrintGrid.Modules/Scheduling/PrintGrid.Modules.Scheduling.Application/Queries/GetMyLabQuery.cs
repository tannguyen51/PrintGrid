using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetMyLabQuery(Guid UserId, bool AllowUnclaimedFallback = true) : IRequest<Result<LabDto?>>;

public class GetMyLabQueryHandler : IRequestHandler<GetMyLabQuery, Result<LabDto?>>
{
    private readonly ILabRepository _labs;

    public GetMyLabQueryHandler(ILabRepository labs) => _labs = labs;

    public async Task<Result<LabDto?>> Handle(GetMyLabQuery query, CancellationToken cancellationToken)
    {
        var owned = await _labs.GetByOwnerUserIdAsync(query.UserId, cancellationToken);
        if (owned is not null)
            return Result.Success<LabDto?>(LabMappings.ToDto(owned));

        if (query.AllowUnclaimedFallback)
        {
            var all = await _labs.GetAllAsync(cancellationToken);
            var unclaimed = all.FirstOrDefault(l => l.OwnerUserId is null);
            if (unclaimed is not null)
                return Result.Success<LabDto?>(LabMappings.ToDto(unclaimed));
        }

        return Result.Success<LabDto?>(null);
    }
}

