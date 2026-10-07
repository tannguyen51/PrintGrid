using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;

/// <summary>
/// FR-HUB-003 / BR-RESCHED-003: create the URGENT reprint for a failed job. The reprint
/// inherits the original deadline and its cost is charged to the at-fault party.
/// </summary>
public record CreateUrgentReprintCommand(
    Guid JobId,
    string Reason,
    Guid? FaultLabId = null) : IRequest<Result<UrgentReprintResultDto>>;

public record UrgentReprintResultDto(
    Guid OriginalJobId,
    Guid? ReprintJobId,
    bool EscalatedToOps,
    DateTime? PlannedStartUtc,
    DateTime? PlannedEndUtc,
    decimal? Score);
