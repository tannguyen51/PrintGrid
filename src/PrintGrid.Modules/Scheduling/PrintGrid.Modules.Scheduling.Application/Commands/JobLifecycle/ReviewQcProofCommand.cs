using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public record ReviewQcProofCommand(
    Guid JobId,
    Guid StaffId,
    bool Approved,
    string? Reason) : IRequest<Result>;
