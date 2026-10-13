using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SetLabStandingCommand(Guid LabId, bool IsInGoodStanding) : IRequest<Result>;
