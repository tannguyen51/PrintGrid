namespace PrintGrid.Modules.Scheduling.Domain.Enums;

/// <summary>
/// Job priority (BR-RESCHED-003). A reprint created after a fault is URGENT so the
/// assign engine treats it as a same-deadline job rather than ordinary queue work.
/// </summary>
public enum JobPriority
{
    Normal,
    Urgent
}
