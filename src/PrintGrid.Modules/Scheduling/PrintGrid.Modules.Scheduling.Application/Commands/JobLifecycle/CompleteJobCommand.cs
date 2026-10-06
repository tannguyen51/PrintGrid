using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record CompleteJobCommand(Guid JobId, int ActualPrintMinutes) : IRequest<Result>;