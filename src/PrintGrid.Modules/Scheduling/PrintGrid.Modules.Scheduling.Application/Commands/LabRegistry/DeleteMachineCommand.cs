using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public sealed record DeleteMachineCommand(Guid LabId, Guid MachineId, Guid ActorUserId) : IRequest<Result>;
