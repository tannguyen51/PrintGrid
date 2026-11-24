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
public class CreateUrgentReprintCommandHandler(
    IJobRepository jobs,
    IAssignmentEngine engine,
    IOpsEscalationRepository escalations,
    IPlacementShortfallService shortfall,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<CreateUrgentReprintCommandHandler> logger)
    : IRequestHandler<CreateUrgentReprintCommand, Result<UrgentReprintResultDto>>
{
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
            FaultAttribution.Hub or FaultAttribution.Customer => null,
            _ => command.FaultLabId ?? original.LabId
        };

    /// <summary>
    /// Who pays for the reprint (BR-RESCHED-003, decided 07/10): the lab pays when it caused
    /// the fault, the platform pays when the hub did, and a customer-caused reprint is billed
    /// to the customer. An ops-triggered reprint keeps the documented default (the lab).
    /// </summary>
    private static CostBearer ResolveCostBearer(CreateUrgentReprintCommand command) =>
        command.Fault switch
        {
            FaultAttribution.Hub => CostBearer.System,
            FaultAttribution.Customer => CostBearer.Customer,
            _ => CostBearer.Lab
        };

    public async Task<Result<UrgentReprintResultDto>> Handle(
        CreateUrgentReprintCommand command,
        CancellationToken cancellationToken)
    {
        var original = await jobs.GetByIdAsync(command.JobId, cancellationToken);
        if (original is null)
            return Result.Failure<UrgentReprintResultDto>(Error.NotFound("Job", command.JobId));

        if (original.Status != JobStatus.Failed)
        {
            return Result.Failure<UrgentReprintResultDto>(
                Error.Conflict($"Job in state {original.Status} cannot be reprinted"));
        }

        var siblings = await jobs.GetByOrderItemIdAsync(original.OrderItemId, cancellationToken);
        var existing = JobReprintLinks.FindReplacement(original, siblings);
        if (existing is not null)
            return ExistingAttempt(existing);

        var reprintIndex = original.ReprintIndex + 1;

        // BR-RESCHED-004 (B14, chốt 23/09): past the cap, auto-reprint stops and a human decides.
        if (reprintIndex > ReprintPolicy.MaxAutoReprints)
        {
            var reason =
                $"Reprint limit reached ({ReprintPolicy.MaxAutoReprints}) for job {original.Id}: {command.Reason}";

            await escalations.AddAsync(
                OpsEscalation.Raise(
                    original.Id,
                    original.OrderItemId,
                    OpsEscalationKind.ReprintLimitExceeded,
                    reason,
                    clock.UtcNow),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Auto-reprint refused for job {JobId}: reprint #{ReprintIndex} exceeds the cap of {Cap}. Flagged for operations",
                original.Id, reprintIndex, ReprintPolicy.MaxAutoReprints);

            return Result.Success(new UrgentReprintResultDto(true, null, null));
        }

        var reprint = Job.CreateUrgentReprint(
            original,
            reprintIndex,
            ResolveFaultLabId(command, original),
            ResolveCostBearer(command),
            createdAtUtc: clock.UtcNow);

        // Commit the reprint job before placing it: the job is a fact of its own. This also
        // keeps the engine's collision retry (which detaches pending changes) from discarding
        // a row that was never saved.
        await jobs.AddAsync(reprint, cancellationToken);
        // The unique ReprintOfJobId index also protects requests that both passed the read
        // above. A rejected insert must never reach the assignment engine or create a shortfall.
        if (!await unitOfWork.TrySaveChangesSafeAsync(cancellationToken))
        {
            // A concurrent request may have committed the same replacement. Reuse its
            // result, without assigning it a second time or changing cost attribution.
            existing = JobReprintLinks.FindReplacement(original,
                await jobs.GetByOrderItemIdAsync(original.OrderItemId, cancellationToken));
            if (existing is not null) return ExistingAttempt(existing);
            return Result.Failure<UrgentReprintResultDto>(Error.Conflict(
                "Không thể tạo thêm job in lại. Vui lòng tải lại để kiểm tra lần in lại đã được tạo."));
        }

        var outcome = await engine.AssignAsync(
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
            logger.LogInformation(
                "Reprint #{ReprintIndex} for job {OriginalJobId} placed as job {ReprintJobId} at {Start:u} (deadline inherited: {DueDate})",
                reprintIndex, original.Id, reprint.Id, outcome.Result!.PlannedStartUtc, reprint.InternalDueDate);
        }
        else if (outcome.Outcome is AssignmentOutcomes.NoFeasibleSlot or AssignmentOutcomes.NoCapableMachine)
        {
            logger.LogWarning(
                "Reprint #{ReprintIndex} for job {OriginalJobId} cannot hold the inherited deadline: {Reason}",
                reprintIndex, original.Id, outcome.Reason);

            await shortfall.HandleAsync(
                reprint,
                outcome.Reason ?? "Reprint cannot hold the inherited deadline",
                cancellationToken);
        }
        else
        {
            // A lost placement race is not a shortfall — the reprint simply stays Pending and
            // can be placed again from the ops board.
            logger.LogWarning(
                "Reprint {ReprintJobId} left unplaced ({Outcome}) — retry from the ops board",
                reprint.Id, outcome.Outcome);
        }

        return Result.Success(new UrgentReprintResultDto(false,
            outcome.Result?.PlannedStartUtc,
            outcome.Result?.PlannedEndUtc,
            reprint.Id));
    }

    private static Result<UrgentReprintResultDto> ExistingAttempt(Job existing) => Result.Success(
        new UrgentReprintResultDto(false, existing.PlannedStartUtc, existing.PlannedEndUtc, existing.Id));
}
