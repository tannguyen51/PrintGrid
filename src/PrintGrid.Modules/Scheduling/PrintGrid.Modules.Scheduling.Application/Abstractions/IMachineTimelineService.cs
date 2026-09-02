using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

public interface IMachineTimelineService
{
    Task<DateTime> FindEarliestFreeSlotAsync(
        Machine machine,
        int durationMinutes,
        DateTime notBeforeUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> GetUtilizationAsync(
        Machine machine,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken cancellationToken = default);
}
