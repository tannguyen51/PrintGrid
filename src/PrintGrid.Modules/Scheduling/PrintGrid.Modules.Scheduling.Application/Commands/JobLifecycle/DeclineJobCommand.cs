using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record DeclineJobCommand(Guid JobId, string Reason) : IRequest<Result>;
