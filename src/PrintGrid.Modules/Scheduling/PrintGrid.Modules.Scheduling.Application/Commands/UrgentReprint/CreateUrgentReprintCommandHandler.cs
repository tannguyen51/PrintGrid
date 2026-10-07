using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;

/// <summary>
/// FR-HUB-003 / BR-RESCHED-003/004 ("in lại khẩn"):
/// the reprint inherits the original deadline so the customer's promise is preserved, it is
/// placed immediately through the same engine the rest of the platform uses, and after
/// <see cref="ReprintPolicy.MaxAutoReprints"/> attempts the case goes to a human instead.
/// </summary>
public class CreateUrgentReprintCommandHandler
    : IRequestHandler<CreateUrgentReprintCommand, Result<UrgentReprintResultDto>>
{
    private readonly IJobRepository _jobs;
    private readonly IAssignmentEngine _engine;
    private readonly IOpsEscalationRepository _escalations;
    private readonly IPlacementShortfallService _shortfall;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CreateUrgentReprintCommandHandler> _logger;

    public CreateUrgentReprintCommandHandler(
        IJobRepository jobs,
        IAssignmentEngine engine,
        IOpsEscalationRepository escalations,
        IPlacementShortfallService shortfall,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<CreateUrgentReprintCommandHandler> logger)
    {
        _jobs = jobs;
        _engine = engine;
        _escalations = escalations;
        _shortfall = shortfall;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Who is at fault decides both the cost and whether a lab is kept out of the replan:
    /// a lab fault blames the lab that printed the original, a hub fault blames nobody in the
    /// network, and an ops-triggered reprint without an explicit lab follows the contract
    /// default (the lab that printed the original).
    /// </summary>
    private static Guid? ResolveFaultLabId(CreateUrgentReprintCommand command, Job original) =>
        command.Fault switch
        {
            FaultAttribution.Lab => original.LabId,
            FaultAttribution.Hub => null,
            FaultAttribution.Customer => null,
            _ => command.FaultLabId ?? original.LabId
        };

    public async Task<Result<UrgentReprintResultDto>> Handle(
        CreateUrgentReprintCommand command,
        CancellationToken cancellationToken)
    {
        var original = await _jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (original is null)
            return Result.Failure<UrgentReprintResultDto>(Error.NotFound("Job", command.JobId));

        if (original.Status != JobStatus.Failed)
            return Result.Failure<UrgentReprintResultDto>(
                Error.Conflict($"Job in state {original.Status} cannot be reprinted"));

        var reprintIndex = original.ReprintIndex + 1;

        // BR-RESCHED-004 (B14, chốt 23/09): past the cap, auto-reprint stops and a human decides.
        if (reprintIndex > ReprintPolicy.MaxAutoReprints)
        {
            var reason =
                $"Reprint limit reached ({ReprintPolicy.MaxAutoReprints}) for job {original.Id}: {command.Reason}";

            await _escalations.AddAsync(
                OpsEscalation.Raise(
                    original.Id,
                    original.OrderItemId,
                    OpsEscalationKind.ReprintLimitExceeded,
                    reason,
                    _clock.UtcNow),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Auto-reprint refused for job {JobId}: reprint #{ReprintIndex} exceeds the cap of {Cap}. Flagged for operations",
                original.Id, reprintIndex, ReprintPolicy.MaxAutoReprints);

            return Result.Success(new UrgentReprintResultDto(original.Id, null, true, null, null, null));
        }

        var reprint = Job.CreateUrgentReprint(
            original,
            reprintIndex,
            ResolveFaultLabId(command, original),
            createdAtUtc: _clock.UtcNow);

        // Commit the reprint job before placing it: the job is a fact of its own. This also
        // keeps the engine's collision retry (which detaches pending changes) from discarding
        // a row that was never saved.
        await _jobs.AddAsync(reprint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var outcome = await _engine.AssignAsync(
            reprint,
            new AssignmentRequest(
                AssignmentTriggers.UrgentReprint,
                TimeBudget: TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds),
                // The lab that produced the defective part does not get to redo it — the same
                // rule that applies to a refusal. If it is the only capable lab the engine
                // keeps it rather than lose feasibility (BR-SCHED-006).
                ExcludeLabId: reprint.FaultLabId),
            cancellationToken);

        if (outcome.Assigned)
        {
            _logger.LogInformation(
                "Reprint #{ReprintIndex} for job {OriginalJobId} placed as job {ReprintJobId} at {Start:u} (deadline inherited: {DueDate})",
                reprintIndex, original.Id, reprint.Id, outcome.Result!.PlannedStartUtc, reprint.InternalDueDate);
        }
        else if (outcome.Outcome is AssignmentOutcomes.NoFeasibleSlot or AssignmentOutcomes.NoCapableMachine)
        {
            _logger.LogWarning(
                "Reprint #{ReprintIndex} for job {OriginalJobId} cannot hold the inherited deadline: {Reason}",
                reprintIndex, original.Id, outcome.Reason);

            await _shortfall.HandleAsync(
                reprint,
                outcome.Reason ?? "Reprint cannot hold the inherited deadline",
                cancellationToken);
        }
        else
        {
            // A lost placement race is not a shortfall — the reprint simply stays Pending and
            // can be placed again from the ops board.
            _logger.LogWarning(
                "Reprint {ReprintJobId} left unplaced ({Outcome}) — retry from the ops board",
                reprint.Id, outcome.Outcome);
        }

        return Result.Success(new UrgentReprintResultDto(
            original.Id,
            reprint.Id,
            false,
            outcome.Result?.PlannedStartUtc,
            outcome.Result?.PlannedEndUtc,
            outcome.Result?.Score));
    }
}
