using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public record SetLabCapacityLimitCommand(Guid LabId, int? MaxConcurrentJobs) : IRequest<Result>;
