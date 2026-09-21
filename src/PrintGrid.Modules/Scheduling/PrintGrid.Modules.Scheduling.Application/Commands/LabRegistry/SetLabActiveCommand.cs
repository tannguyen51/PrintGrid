using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SetLabActiveCommand(Guid LabId, bool IsActive) : IRequest<Result>;