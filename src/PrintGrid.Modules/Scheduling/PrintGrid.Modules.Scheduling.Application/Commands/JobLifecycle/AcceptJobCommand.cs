using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record AcceptJobCommand(Guid JobId) : IRequest<Result>;
