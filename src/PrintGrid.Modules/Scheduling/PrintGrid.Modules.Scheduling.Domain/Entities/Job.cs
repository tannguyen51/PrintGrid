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
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result AssignTo(Guid labId, Guid machineId, DateTime plannedStartUtc, DateTime plannedEndUtc, decimal score)
    {
        if (Status is not (JobStatus.Pending or JobStatus.Reassigned))
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot be assigned"));
        if (plannedEndUtc <= plannedStartUtc)
            return Result.Failure(Error.Validation("Planned end must be after planned start"));

        LabId = labId;
        MachineId = machineId;
        PlannedStartUtc = plannedStartUtc;
        PlannedEndUtc = plannedEndUtc;
        Status = JobStatus.Assigned;

        AddDomainEvent(new JobAssignedEvent(Id, labId, machineId, plannedStartUtc, plannedEndUtc, score));
        return Result.Success();
    }

    public Result Accept()
    {
        if (Status != JobStatus.Assigned)
            return Result.Failure(Error.Conflict("Only an assigned job can be accepted"));

        Status = JobStatus.Accepted;
        return Result.Success();
    }

    public Result Start(DateTime startedAtUtc)
    {
        if (Status != JobStatus.Accepted)
            return Result.Failure(Error.Conflict("Only an accepted job can start printing"));

        Status = JobStatus.InProgress;
        StartedAtUtc = startedAtUtc;
        return Result.Success();
    }

    public Result Complete(DateTime completedAtUtc, int actualPrintMinutes)
    {
        if (Status != JobStatus.InProgress)
            return Result.Failure(Error.Conflict("Only an in-progress job can complete"));

        Status = JobStatus.AwaitingInspection;
        CompletedAtUtc = completedAtUtc;
        ActualPrintMinutes = actualPrintMinutes;
        return Result.Success();
    }

    public Result Fail(string reason)
    {
        if (Status is JobStatus.Completed or JobStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Job in state {Status} cannot fail"));

        Status = JobStatus.Failed;
        FailureReason = reason;

        AddDomainEvent(new JobFailedEvent(Id, LabId!.Value, MachineId!.Value, reason, AttemptNumber));
        AddDomainEvent(new ReschedulingTriggeredEvent(Id, "print_failure", InternalDueDate));
        return Result.Success();
    }

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

    public void MarkInspectionPassed() => Status = JobStatus.Completed;
}
