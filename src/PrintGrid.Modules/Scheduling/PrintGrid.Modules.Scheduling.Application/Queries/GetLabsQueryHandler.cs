using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetLabsQueryHandler(ILabRepository labs) : IRequestHandler<GetLabsQuery, Result<IReadOnlyList<LabDto>>>
{
    public async Task<Result<IReadOnlyList<LabDto>>> Handle(
        GetLabsQuery query,
        CancellationToken cancellationToken)
    {
        var entities = query.OwnerUserId.HasValue
            ? await labs.GetByOwnerAsync(query.OwnerUserId.Value, cancellationToken)
            : query.IncludeInactive
                ? await labs.GetAllAsync(cancellationToken)
                : await labs.GetActiveWithMachinesAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LabDto>>(entities.Select(LabMappings.ToDto).ToList());
    }
}
