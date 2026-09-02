namespace PrintGrid.Modules.Scheduling.Domain.Enums;

public enum JobStatus
{
    Pending,
    Assigned,
    Accepted,
    InProgress,
    AwaitingInspection,
    Completed,
    Failed,
    Reassigned,
    Cancelled
}
