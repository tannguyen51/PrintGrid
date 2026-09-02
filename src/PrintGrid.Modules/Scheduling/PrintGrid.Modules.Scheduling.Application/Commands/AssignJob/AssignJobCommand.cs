using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;

public record AssignJobCommand(Guid JobId) : IRequest<Result<AssignmentResultDto>>;

public record AssignmentResultDto(
    Guid JobId,
    Guid LabId,
    Guid MachineId,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    decimal Score,
    IReadOnlyDictionary<string, decimal> ScoreBreakdown);
