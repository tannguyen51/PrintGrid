using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Job : AggregateRoot<Guid>
{
    public Guid OrderItemId { get; private set; }
    public Guid ModelId { get; private set; }
    public JobSpecification Specification { get; private set; } = null!;
    public JobStatus Status { get; private set; }
    public DateOnly InternalDueDate { get; private set; }
    public int EstimatedPrintMinutes { get; private set; }
    public int? ActualPrintMinutes { get; private set; }
    public int AttemptNumber { get; private set; }

    public Guid? LabId { get; private set; }
    public Guid? MachineId { get; private set; }
    public DateTime? PlannedStartUtc { get; private set; }
    public DateTime? PlannedEndUtc { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Normal for originally placed work, Urgent for reprints (BR-RESCHED-003).</summary>
    public JobPriority Priority { get; private set; } = JobPriority.Normal;

    /// <summary>Root job of a reprint chain; null when this job is the original (BR-RESCHED-003).</summary>
    public Guid? OriginalJobId { get; private set; }

    /// <summary>0 for an original job, 1..N for successive reprints of it (BR-RESCHED-004 cap).</summary>
    public int ReprintIndex { get; private set; }

    /// <summary>Who pays for this job (13-AC: a reprint after a fault is charged to the lab).</summary>
    public CostBearer CostBearer { get; private set; } = CostBearer.Customer;

    /// <summary>Lab held responsible for a reprint, when one can be identified.</summary>
    public Guid? FaultLabId { get; private set; }

    private Job() { }

    public static Job Create(
        Guid orderItemId,
        Guid modelId,
        JobSpecification specification,
        int estimatedPrintMinutes,
        DateOnly internalDueDate)
    {
        if (estimatedPrintMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedPrintMinutes), "Estimate must be positive");

        return new Job
        {
            Id = Guid.NewGuid(),
            OrderItemId = orderItemId,
            ModelId = modelId,
            Specification = specification,
            EstimatedPrintMinutes = estimatedPrintMinutes,
            InternalDueDate = internalDueDate,
            Status = JobStatus.Pending,
            AttemptNumber = 1,
            Priority = JobPriority.Normal,
            ReprintIndex = 0,
            CostBearer = CostBearer.Customer,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates the reprint job for a failed original (FR-HUB-003 / BR-RESCHED-003): same
    /// specification, URGENT priority, and the ORIGINAL internal due date inherited so the
    /// customer's promised delivery date is preserved. Cost is charged to the at-fault
    /// party (default: the lab, per 13-Acceptance-Criteria.md:330).
    /// </summary>
    public static Job CreateUrgentReprint(
        Job original,
        int reprintIndex,
        Guid? faultLabId = null,
        CostBearer costBearer = CostBearer.Lab,
        DateTime? createdAtUtc = null)
    {
        // Rebuild (never share) the owned value objects: an EF owned instance cannot be
        // tracked by two owners at once.
        var volume = original.Specification.RequiredVolume;
        var specification = JobSpecification.Create(
            BuildVolume.Create(volume.WidthMm, volume.DepthMm, volume.HeightMm),
            original.Specification.MaterialCode,
            original.Specification.ColorCode,
            original.Specification.LayerHeightMm,
            original.Specification.ToleranceMm,
            original.Specification.Technology,
            original.Specification.MaterialGrams);

        var reprint = new Job
        {
            Id = Guid.NewGuid(),
            OrderItemId = original.OrderItemId,
            ModelId = original.ModelId,
            Specification = specification,
            EstimatedPrintMinutes = original.EstimatedPrintMinutes,
            InternalDueDate = original.InternalDueDate,
            Status = JobStatus.Pending,
            AttemptNumber = 1,
            Priority = JobPriority.Urgent,
            OriginalJobId = original.OriginalJobId ?? original.Id,
            ReprintIndex = reprintIndex,
            CostBearer = costBearer,
            FaultLabId = faultLabId ?? original.LabId,
            CreatedAt = createdAtUtc ?? DateTime.UtcNow
        };

        reprint.AddDomainEvent(new JobReprintCreatedEvent(
            reprint.Id,
            reprint.OriginalJobId!.Value,
            reprintIndex,
            reprint.FaultLabId,
            reprint.InternalDueDate));

        return reprint;
    }

    public Result AssignTo(Guid labId, Guid machineId, DateTime plannedStartUtc, DateTime plannedEndUtc, decimal score, DateTime? assignedAtUtc = null)
    {
        if (Status is not (JobStatus.Pending or JobStatus.Reassigned))
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot be assigned"));
        if (plannedEndUtc <= plannedStartUtc)
            return Result.Failure(Error.Validation("Planned end must be after planned start"));

        LabId = labId;
        MachineId = machineId;
        PlannedStartUtc = plannedStartUtc;
        PlannedEndUtc = plannedEndUtc;
        AssignedAtUtc = assignedAtUtc ?? DateTime.UtcNow;
        Status = JobStatus.Assigned;

        AddDomainEvent(new JobAssignedEvent(Id, labId, machineId, plannedStartUtc, plannedEndUtc, score));
        return Result.Success();
    }

    public Result Accept() => Accept(DateTime.UtcNow);

    public Result Accept(DateTime nowUtc)
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure(Error.Conflict("Only an assigned job can be accepted"));

        if (AssignedAtUtc.HasValue && (nowUtc - AssignedAtUtc.Value) > TimeSpan.FromHours(2))
        {
            Decline("Acceptance window expired (2 hours timeout)");
            return Result.Failure(Error.Conflict("Acceptance window expired; job returned to pending"));
        }

        Status = JobStatus.Accepted;
        return Result.Success();
    }

    public Result Decline(string reason)
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure(Error.Conflict("Only an assigned job can be declined"));

        var declinedLabId = LabId ?? Guid.Empty;
        Status = JobStatus.Pending;
        LabId = null;
        MachineId = null;
        PlannedStartUtc = null;
        PlannedEndUtc = null;
        AssignedAtUtc = null;
        FailureReason = reason;

        AddDomainEvent(new JobDeclinedEvent(Id, declinedLabId, reason));
        AddDomainEvent(new ReschedulingTriggeredEvent(
            Id,
            "lab_decline",
            InternalDueDate,
            ExcludedLabId: declinedLabId == Guid.Empty ? null : declinedLabId));
        return Result.Success();
    }

    public Result TimeoutAcceptance(DateTime nowUtc)
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure(Error.Conflict("Only an assigned job can time out"));

        if (AssignedAtUtc.HasValue && (nowUtc - AssignedAtUtc.Value) < TimeSpan.FromHours(2))
            return Result.Failure(Error.Conflict("Job acceptance window has not expired yet"));

        return Decline("Acceptance window expired (2 hours timeout)");
    }

    public Result Start(DateTime startedAtUtc)
    {
        if (Status != JobStatus.Accepted)
            return Result.Failure(Error.Conflict("Only an accepted job can start printing"));

        Status = JobStatus.InProgress;
        StartedAtUtc = startedAtUtc;

        AddDomainEvent(new JobStartedEvent(Id, OrderItemId));
        return Result.Success();
    }

    public Result Complete(DateTime completedAtUtc, int actualPrintMinutes)
    {
        if (Status != JobStatus.InProgress)
            return Result.Failure(Error.Conflict("Only an in-progress job can complete"));

        Status = JobStatus.AwaitingInspection;
        CompletedAtUtc = completedAtUtc;
        ActualPrintMinutes = actualPrintMinutes;

        AddDomainEvent(new JobCompletedEvent(Id, OrderItemId));
        return Result.Success();
    }

    public Result Fail(string reason)
    {
        if (Status is JobStatus.Completed or JobStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot fail"));

        Status = JobStatus.Failed;
        FailureReason = reason;

        // LabId/MachineId can legitimately be null (failing a job that was never placed);
        // Guid.Empty keeps the event well-formed instead of throwing.
        AddDomainEvent(new JobFailedEvent(Id, LabId ?? Guid.Empty, MachineId ?? Guid.Empty, reason, AttemptNumber, OrderItemId));
        AddDomainEvent(new ReschedulingTriggeredEvent(Id, "print_failure", InternalDueDate));
        return Result.Success();
    }

    /// <summary>
    /// Returns a failed job to the placement queue so the SAME job can be printed again.
    /// The automatic after-fault path does not use this any more: FR-HUB-003 / BR-RESCHED-003
    /// create a separate URGENT reprint job that inherits the original deadline. Kept as the
    /// manual/ops alternative (and the only producer of <see cref="JobStatus.Reassigned"/>).
    /// </summary>
    public Result PrepareForReassignment()
    {
        if (Status != JobStatus.Failed)
            return Result.Failure(Error.Conflict("Only a failed job can be reassigned"));

        AttemptNumber++;
        Status = JobStatus.Reassigned;
        LabId = null;
        MachineId = null;
        PlannedStartUtc = null;
        PlannedEndUtc = null;
        StartedAtUtc = null;
        return Result.Success();
    }

    public void MarkInspectionPassed()
    {
        Status = JobStatus.Completed;
        // Optionally AddDomainEvent(new JobInspectionPassedEvent) if needed, 
        // but AwaitingInspection -> Completed is handled here. Let's just rely on JobCompletedEvent for now,
        // or add an event if we need to track QualityCheck. Let's add it.
    }
}
