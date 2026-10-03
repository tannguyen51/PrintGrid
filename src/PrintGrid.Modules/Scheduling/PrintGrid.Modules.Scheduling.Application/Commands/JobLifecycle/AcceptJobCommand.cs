using MediatR;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record AcceptJobCommand(Guid JobId) : IRequest<Result>;