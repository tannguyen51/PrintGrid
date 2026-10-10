using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

/// <summary>The operations queue: reprints past the cap and jobs with no feasible date (FR-ANAL-004).</summary>
public record GetOpsEscalationsQuery : IRequest<Result<IReadOnlyList<OpsEscalationDto>>>;

public class GetOpsEscalationsQueryHandler
    : IRequestHandler<GetOpsEscalationsQuery, Result<IReadOnlyList<OpsEscalationDto>>>
{
    private readonly IOpsEscalationRepository _escalations;

    public GetOpsEscalationsQueryHandler(IOpsEscalationRepository escalations)
    {
        _escalations = escalations;
    }

    public async Task<Result<IReadOnlyList<OpsEscalationDto>>> Handle(
        GetOpsEscalationsQuery query,
        CancellationToken cancellationToken)
    {
        var open = await _escalations.GetOpenAsync(cancellationToken);

        IReadOnlyList<OpsEscalationDto> dtos = open
            .Select(e => new OpsEscalationDto(
                e.Id,
                e.JobId,
                e.OrderItemId,
                e.Kind.ToString(),
                e.Reason,
                e.Status.ToString(),
                e.CreatedAtUtc))
            .ToList();

        return Result.Success(dtos);
    }
}
