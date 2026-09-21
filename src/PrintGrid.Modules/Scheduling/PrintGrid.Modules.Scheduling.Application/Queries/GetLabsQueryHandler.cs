using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetLabsQueryHandler : IRequestHandler<GetLabsQuery, Result<IReadOnlyList<LabDto>>>
{
    private readonly ILabRepository _labs;

    public GetLabsQueryHandler(ILabRepository labs) => _labs = labs;

    public async Task<Result<IReadOnlyList<LabDto>>> Handle(
        GetLabsQuery query,
        CancellationToken cancellationToken)
    {
        var entities = query.IncludeInactive
            ? await _labs.GetAllAsync(cancellationToken)
            : await _labs.GetActiveWithMachinesAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LabDto>>(entities.Select(LabMappings.ToDto).ToList());
    }
}