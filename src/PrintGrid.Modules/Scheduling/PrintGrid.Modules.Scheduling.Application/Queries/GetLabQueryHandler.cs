using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public class GetLabQueryHandler(ILabRepository labs) : IRequestHandler<GetLabQuery, Result<LabDto>>
{
    public async Task<Result<LabDto>> Handle(GetLabQuery query, CancellationToken cancellationToken)
    {
        var lab = await labs.GetByIdAsync(query.LabId, cancellationToken);
        return lab is null
            ? Result.Failure<LabDto>(Error.NotFound("Lab", query.LabId))
            : Result.Success(LabMappings.ToDto(lab));
    }
}
