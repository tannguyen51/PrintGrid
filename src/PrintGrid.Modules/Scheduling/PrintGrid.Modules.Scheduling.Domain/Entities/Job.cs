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
    public decimal? ActualMaterialGrams { get; private set; }
    public int AttemptNumber { get; private set; }
    public int Quantity { get; private set; }
    public Guid? ParentJobId { get; private set; }

    public Guid? LabId { get; private set; }
    public Guid? MachineId { get; private set; }
    public DateTime? PlannedStartUtc { get; private set; }
    public DateTime? PlannedEndUtc { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public QcProofStatus QcProofStatus { get; private set; }
    public string? QcSelfReport { get; private set; }
    public string? QcProofPhotoKeys { get; private set; }
    public Guid? QcReviewedBy { get; private set; }
    public DateTime? QcReviewedAtUtc { get; private set; }
    public string? QcRejectionReason { get; private set; }
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
        DateOnly internalDueDate, int quantity = 1, Guid? parentJobId = null)
    {
        if (estimatedPrintMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedPrintMinutes), "Estimate must be positive");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

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
            Quantity = quantity,
            ParentJobId = parentJobId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates the reprint job for a failed original (FR-HUB-003 / BR-RESCHED-003): same
    /// specification, URGENT priority, and the ORIGINAL internal due date inherited so the
    /// customer's promised delivery date is preserved. Cost is charged to the at-fault
    /// party (default: the lab, per 13-Acceptance-Criteria.md:330).
    /// <paramref name="faultLabId"/> is stored exactly as given — the caller decides who is
    /// at fault, including "nobody in the network" (null) for a hub-caused failure.
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
            FaultLabId = faultLabId,
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

    /// <summary>
    /// Splits an unstarted job across labs to protect a committed date (BR-SCHED-010, the
    /// first step of the FR-SCHED-007 repair order of preference). The original is cancelled
    /// and one child job per quantity is returned; pricing is untouched.
    /// </summary>
    public Result<IReadOnlyList<Job>> Split(IReadOnlyList<int> quantities)
    {
        if (Status is not (JobStatus.Pending or JobStatus.Reassigned)) return Result.Failure<IReadOnlyList<Job>>(Error.Conflict("Only an unstarted job can be split"));
        if (Quantity < 2 || quantities.Count < 2 || quantities.Any(x => x <= 0) || quantities.Sum() != Quantity) return Result.Failure<IReadOnlyList<Job>>(Error.Validation("Split quantities must be positive and sum to the original quantity"));
        var jobs = quantities.Select(q =>
        {
            var ratio = (decimal)q / Quantity;
            // Rebuild the owned value objects per child: an EF owned instance cannot be tracked
            // by two owners at once (BR-SCHED-010 split batches).
            var spec = JobSpecification.Create(
                BuildVolume.Create(Specification.RequiredVolume.WidthMm, Specification.RequiredVolume.DepthMm, Specification.RequiredVolume.HeightMm),
                Specification.MaterialCode,
                Specification.ColorCode,
                Specification.LayerHeightMm,
                Specification.ToleranceMm,
                Specification.Technology,
                decimal.Round(Specification.MaterialGrams * ratio, 2));
            return Create(OrderItemId, ModelId, spec, Math.Max(1, (int)Math.Ceiling(EstimatedPrintMinutes * ratio)), InternalDueDate, q, Id);
        }).ToList();
        Status = JobStatus.Cancelled; FailureReason = "Split into quantity batches due to schedule risk";
        return Result.Success<IReadOnlyList<Job>>(jobs);
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

    /// <summary>
    /// Lab completion (FR-LAB-004): the lab reports what it actually printed, with the QC
    /// evidence the hub will inspect (photos are mandatory, BR-QC-003).
    /// </summary>
    public Result Complete(
        DateTime completedAtUtc,
        int actualPrintMinutes,
        string selfReport,
        IReadOnlyCollection<string> photoKeys,
        decimal? actualMaterialGrams = null)
    {
        if (Status != JobStatus.InProgress)
            return Result.Failure(Error.Conflict("Only an in-progress job can complete"));
        if (actualPrintMinutes <= 0)
            return Result.Failure(Error.Validation("Actual print time must be positive"));
        if (string.IsNullOrWhiteSpace(selfReport))
            return Result.Failure(Error.Validation("QC self-report is required"));
        if (photoKeys.Count is < 1 or > 5 || photoKeys.Any(string.IsNullOrWhiteSpace))
            return Result.Failure(Error.Validation("Between 1 and 5 QC proof photos are required"));
        if (actualMaterialGrams is <= 0)
            return Result.Failure(Error.Validation("Actual material usage must be positive"));

        Status = JobStatus.AwaitingInspection;
        CompletedAtUtc = completedAtUtc;
        ActualPrintMinutes = actualPrintMinutes;
        QcSelfReport = selfReport.Trim();
        QcProofPhotoKeys = string.Join('|', photoKeys);
        QcProofStatus = QcProofStatus.Pending;
        QcReviewedBy = null;
        QcReviewedAtUtc = null;
        QcRejectionReason = null;
        ActualMaterialGrams = actualMaterialGrams;

        AddDomainEvent(new JobCompletedEvent(Id, OrderItemId));
        return Result.Success();
    }

    public IReadOnlyList<string> GetQcProofPhotoKeys() =>
        string.IsNullOrWhiteSpace(QcProofPhotoKeys)
            ? []
            : QcProofPhotoKeys.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public Result ReviewQcProof(bool approved, Guid staffId, string? reason, DateTime reviewedAtUtc)
    {
        if (Status != JobStatus.AwaitingInspection || QcProofStatus != QcProofStatus.Pending)
            return Result.Failure(Error.Conflict("Only a pending QC proof can be reviewed"));
        if (!approved && string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("A rejection reason is required"));

        QcReviewedBy = staffId;
        QcReviewedAtUtc = reviewedAtUtc;
        QcProofStatus = approved ? QcProofStatus.Approved : QcProofStatus.Rejected;
        QcRejectionReason = approved ? null : reason!.Trim();

        if (!approved)
            Status = JobStatus.InProgress;

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

    /// <summary>
    /// Hub QC pass (FR-HUB-002): the inspection evidence is recorded and the job closes.
    /// The lab's own QC evidence must already be approved; that is returned as a failure
    /// rather than thrown so the API answers 409 instead of 500.
    /// </summary>
    public Result MarkInspectionPassed(IReadOnlyList<string> photoUrls)
    {
        if (Status != JobStatus.AwaitingInspection)
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot be inspected"));
        if (QcProofStatus != QcProofStatus.Approved)
            return Result.Failure(Error.Conflict("Lab QC proof must be approved before hub inspection"));

        Status = JobStatus.Completed;
        AddDomainEvent(new JobInspectionPassedEvent(Id, photoUrls));
        return Result.Success();
    }

    /// <summary>
    /// Hub QC fail where the customer is at fault (FR-HUB-002 AC02 / BR-QC-004): the job fails,
    /// no reprint is created and the customer is notified that the model file is the problem.
    /// </summary>
    public Result FailInspectionCustomerFault(
        string failureReason,
        IReadOnlyList<string> photoUrls,
        string customerNotificationMessage)
    {
        if (Status != JobStatus.AwaitingInspection)
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot be inspected"));

        Status = JobStatus.Failed;
        FailureReason = failureReason;

        AddDomainEvent(new CustomerFaultInspectionFailedEvent(
            Id,
            failureReason,
            photoUrls,
            customerNotificationMessage));

        return Result.Success();
    }
}
