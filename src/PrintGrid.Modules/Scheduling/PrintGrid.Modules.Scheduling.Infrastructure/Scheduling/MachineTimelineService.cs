using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;

public class MachineTimelineService : IMachineTimelineService
{
    private static readonly JobStatus[] BlockingStatuses =
    {
        JobStatus.Assigned, JobStatus.Accepted, JobStatus.InProgress
    };

    private readonly PrintGridDbContext _context;

    public MachineTimelineService(PrintGridDbContext context) => _context = context;

    public async Task<DateTime> FindEarliestFreeSlotAsync(
        Machine machine,
        int durationMinutes,
        DateTime notBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        var booked = await LoadBookedWindowsAsync(machine.Id, notBeforeUtc, cancellationToken);
        var adjustedDuration = TimeSpan.FromMinutes(durationMinutes / (double)machine.SpeedFactor);
        var cursor = notBeforeUtc;

        foreach (var window in booked)
        {
            if (window.Start - cursor >= adjustedDuration) return cursor;
            if (window.End > cursor) cursor = window.End;
        }

        return cursor;
    }

    public async Task<decimal> GetUtilizationAsync(
        Machine machine,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var totalHours = (decimal)(windowEndUtc - windowStartUtc).TotalHours;
        if (totalHours <= 0) return 1m;

        var booked = await LoadBookedWindowsAsync(machine.Id, windowStartUtc, cancellationToken);

        var busyHours = booked
            .Where(w => w.Start < windowEndUtc)
            .Sum(w => (decimal)(
                Min(w.End, windowEndUtc) - Max(w.Start, windowStartUtc)).TotalHours);

        return Math.Clamp(busyHours / totalHours, 0m, 1m);
    }

    private async Task<List<(DateTime Start, DateTime End)>> LoadBookedWindowsAsync(
        Guid machineId,
        DateTime fromUtc,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Set<Job>()
            .Where(j => j.MachineId == machineId
                        && BlockingStatuses.Contains(j.Status)
                        && j.PlannedEndUtc != null
                        && j.PlannedEndUtc > fromUtc)
            .OrderBy(j => j.PlannedStartUtc)
            .Select(j => new { j.PlannedStartUtc, j.PlannedEndUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => (Start: r.PlannedStartUtc!.Value, End: r.PlannedEndUtc!.Value))
            .ToList();
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
