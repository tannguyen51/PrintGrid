using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;

/// <summary>
/// FR-HUB-003 / BR-RESCHED-003: create the URGENT reprint for a failed job. The reprint
/// inherits the original deadline and its cost is charged to the at-fault party.
/// <paramref name="Fault"/> is set when the caller knows the attribution (hub inspection);
/// <paramref name="FaultLabId"/> is the explicit lab for the ops-triggered path, which
/// defaults to the lab that printed the original job.
/// </summary>
public record CreateUrgentReprintCommand(
    Guid JobId,
    string Reason,
    Guid? FaultLabId = null,
    FaultAttribution? Fault = null) : IRequest<Result<UrgentReprintResultDto>>;

public record UrgentReprintResultDto(
    Guid OriginalJobId,
    Guid? ReprintJobId,
    bool EscalatedToOps,
    DateTime? PlannedStartUtc,
    DateTime? PlannedEndUtc,
    decimal? Score);
