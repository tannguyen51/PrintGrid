using MediatR;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

/// <summary>
/// Lab reports a production failure after accepting the job (machine breakdown, defective
/// print, ran out of material). Mirrors the hub's QC-failure path (FR-HUB-003 / FR-SCHED-007):
/// the job is closed as Failed and an urgent reprint job is created, charged to the lab that
/// caused the fault and excluding that lab from the new placement.
/// </summary>
public record ReportJobFailureCommand(Guid JobId, string Reason) : IRequest<Result<UrgentReprintResultDto>>;

public class ReportJobFailureCommandHandler
    : IRequestHandler<ReportJobFailureCommand, Result<UrgentReprintResultDto>>
{
    private readonly IJobRepository _jobs;
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public ReportJobFailureCommandHandler(
        IJobRepository jobs,
        ILabRepository labs,
        IUnitOfWork unitOfWork,
        ISender sender)
    {
        _jobs = jobs;
        _labs = labs;
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<Result<UrgentReprintResultDto>> Handle(
        ReportJobFailureCommand command,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null)
            return Result.Failure<UrgentReprintResultDto>(Error.NotFound("Job", command.JobId));

        var reason = (command.Reason ?? string.Empty).Trim();
        if (reason.Length < 10)
            return Result.Failure<UrgentReprintResultDto>(
                Error.Validation("Lý do lỗi phải có ít nhất 10 ký tự."));

        // Only work the lab actually took on can be reported as failed; a job still waiting
        // for acceptance should be Declined instead.
        if (job.Status is not (JobStatus.Accepted or JobStatus.InProgress))
            return Result.Failure<UrgentReprintResultDto>(
                Error.Conflict($"Job in state {job.Status} cannot be reported as failed"));

        var failedAtLabId = job.LabId;

        var fail = job.Fail(reason);
        if (fail.IsFailure)
            return Result.Failure<UrgentReprintResultDto>(fail.Error);

        // Close the material reservation: a mid-print failure still consumed what was held.
        // Settling (rather than silently releasing) releases the hold and records the usage.
        if (job.LabId is { } labId)
        {
            var lab = await _labs.GetByIdAsync(labId, cancellationToken);
            var reservedGrams = lab?.MaterialReservations
                .FirstOrDefault(r => r.JobId == job.Id)?.ReservedGrams;

            if (lab is not null && reservedGrams is { } grams)
            {
                try
                {
                    lab.SettleMaterial(job.Id, grams);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<UrgentReprintResultDto>(Error.Conflict(ex.Message));
                }
            }
        }

        // Commit the failure first so the reprint handler sees a Failed job.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _sender.Send(
            new CreateUrgentReprintCommand(
                job.Id,
                reason,
                FaultLabId: failedAtLabId,
                Fault: FaultAttribution.Lab),
            cancellationToken);
    }
}
