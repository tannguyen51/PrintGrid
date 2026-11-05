using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Mappings;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetMyLabQuery(Guid UserId, bool AllowUnclaimedFallback = false) : IRequest<Result<LabDto?>>;

public class GetMyLabQueryHandler(ILabRepository labs) : IRequestHandler<GetMyLabQuery, Result<LabDto?>>
{
    public async Task<Result<LabDto?>> Handle(GetMyLabQuery query, CancellationToken cancellationToken)
    {
        var owned = await labs.GetByOwnerUserIdAsync(query.UserId, cancellationToken);
        if (owned is not null)
            return Result.Success<LabDto?>(LabMappings.ToDto(owned));

        // Operators (and any other team member) reach their lab through lab_staff.
        // There is deliberately no "first unclaimed lab" fallback: that would hand a
        // random lab to an account that was never assigned to it.
        var member = await labs.GetByStaffUserIdAsync(query.UserId, cancellationToken);
        if (member is not null)
            return Result.Success<LabDto?>(LabMappings.ToDto(member));

        if (!query.AllowUnclaimedFallback) return Result.Success<LabDto?>(null);
        var all = await labs.GetAllAsync(cancellationToken);
        var unclaimed = all.FirstOrDefault(l => l.OwnerUserId is null);
        return unclaimed is not null ? Result.Success<LabDto?>(LabMappings.ToDto(unclaimed)) : Result.Success<LabDto?>(null);
    }
}

