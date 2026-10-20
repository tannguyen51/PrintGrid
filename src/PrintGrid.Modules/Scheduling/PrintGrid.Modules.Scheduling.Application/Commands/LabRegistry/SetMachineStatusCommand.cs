using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SetMachineStatusCommand(
    Guid LabId,
    Guid MachineId,
    MachineStatus Status,
    Guid? RequestingUserId = null,
    bool IsAdminOrOps = true,
    Guid? ActorUserId = null) : IRequest<Result>;
