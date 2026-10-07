using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

/// <summary>
/// What happens when the engine cannot place a job before its committed date
/// (BR-SCHED-008 / BR-SCHED-011 / BR-NOTIFY-002): state the earliest date the network CAN
/// meet, ask the customer to approve it, and put the case in the operations queue. Shared by
/// the reschedule path (FR-SCHED-007) and the reprint path (FR-HUB-003 §24.4).
/// </summary>
public interface IPlacementShortfallService
{
    Task HandleAsync(Job job, string reason, CancellationToken cancellationToken = default);
}
