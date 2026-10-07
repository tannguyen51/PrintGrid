using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

/// <summary>
/// Hub QC decision (FR-HUB-002): PASS closes the job; FAIL records the failure and hands off
/// to the urgent-reprint flow (FR-HUB-003), which creates a NEW job that inherits the original
/// deadline and is charged to the at-fault lab.
/// </summary>
public class InspectJobCommandHandler : IRequestHandler<InspectJobCommand, Result>
{
    private readonly IJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly ILogger<InspectJobCommandHandler> _logger;

    public InspectJobCommandHandler(
        IJobRepository jobs,
        IUnitOfWork unitOfWork,
        ISender sender,
        ILogger<InspectJobCommandHandler> logger)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result> Handle(InspectJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (job is null) return Result.Failure(Error.NotFound("Job", command.JobId));

        // Only a job awaiting inspection can be judged — this also stops a retried FAIL from
        // creating a second reprint for the same failed print.
        if (job.Status != JobStatus.AwaitingInspection)
            return Result.Failure(Error.Conflict($"Job in state {job.Status} cannot be inspected"));

        if (command.Passed)
        {
            job.MarkInspectionPassed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var reason = command.Note ?? "Failed hub inspection";
        var failedAtLabId = job.LabId;

        var fail = job.Fail(reason);
        if (fail.IsFailure) return fail;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reprint = await _sender.Send(
            new CreateUrgentReprintCommand(job.Id, reason, failedAtLabId),
            cancellationToken);

        if (reprint.IsFailure)
        {
            // The QC failure itself is committed; what failed is the reprint, which ops must
            // pick up. Surfacing it lets the hub see why no reprint job appeared.
            _logger.LogError(
                "QC failure recorded for job {JobId} but the urgent reprint could not be created: {Code} {Message}",
                job.Id, reprint.Error.Code, reprint.Error.Message);
            return Result.Failure(reprint.Error);
        }

        if (reprint.Value.EscalatedToOps)
        {
            _logger.LogWarning(
                "Job {JobId} failed inspection again; reprint limit reached, escalated to operations",
                job.Id);
        }

        return Result.Success();
    }
}
