using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SetMachineStatusCommand(
    Guid LabId,
    Guid MachineId,
    MachineStatus Status) : IRequest<Result>;