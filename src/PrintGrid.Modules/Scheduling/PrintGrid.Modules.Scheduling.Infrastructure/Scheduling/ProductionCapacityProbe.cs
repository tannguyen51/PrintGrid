using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;

/// <summary>
/// FR-SCHED-005 speculative placement: answers "earliest feasible delivery" by running a
/// trial (read-only — commits nothing, touches no real job) across every capable machine's
/// booked windows, then adds lab→hub transit and a hub processing buffer (BR-SCHED-001
/// backward scheduling basis). Null result = no machine in the network can produce it.
/// </summary>
public class ProductionCapacityProbe : IProductionCapacityProbe
{
    /// <summary>Hub days for QC + consolidation between arrival and ship-out (cfg in FR-ADMIN-002 later).</summary>
    public const int HubProcessingDays = 1;

    private static readonly JobStatus[] BlockingStatuses =
    {
        JobStatus.Assigned, JobStatus.Accepted, JobStatus.InProgress
    };

    private readonly PrintGridDbContext _context;

    public ProductionCapacityProbe(PrintGridDbContext context) => _context = context;

    public async Task<TrialPlacementResult?> FindEarliestFeasibleDeliveryAsync(
        IReadOnlyList<TrialPlacementLine> lines,
        DateTime notBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) return null;

        var specs = new List<JobSpecification>();
        foreach (var line in lines)
        {
            if (!TryBuildSpec(line, out var spec)) return null; // malformed geometry/spec input
            specs.Add(spec);
        }

        var labs = await _context.Set<Lab>()
            .Include(l => l.Machines)
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

        var candidates = labs
            .SelectMany(l => l.Machines.Select(m => (Lab: l, Machine: m)))
            .Where(x => x.Machine.Status is MachineStatus.Idle or MachineStatus.Printing)
            .ToList();

        if (candidates.Count == 0) return null;

        var machineIds = candidates.Select(x => x.Machine.Id).ToList();
        var bookedRows = await _context.Set<Job>()
            .Where(j => j.MachineId != null
                        && machineIds.Contains(j.MachineId.Value)
                        && BlockingStatuses.Contains(j.Status)
                        && j.PlannedEndUtc != null
                        && j.PlannedEndUtc > notBeforeUtc)
            .Select(j => new { j.MachineId, j.PlannedStartUtc, j.PlannedEndUtc })
            .ToListAsync(cancellationToken);

        var bookedByMachine = bookedRows
            .GroupBy(r => r.MachineId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.PlannedStartUtc)
                      .Select(r => (Start: r.PlannedStartUtc!.Value, End: r.PlannedEndUtc!.Value))
                      .ToList());

        TrialPlacementResult? best = null;

        foreach (var (lab, machine) in candidates)
        {
            if (!specs.All(machine.Supports)) continue;

            var windows = bookedByMachine.TryGetValue(machine.Id, out var w)
                ? w
                : new List<(DateTime Start, DateTime End)>();
            var cursor = notBeforeUtc;

            foreach (var (line, spec) in lines.Zip(specs))
            {
                var duration = TimeSpan.FromMinutes(line.TotalMinutes / (double)machine.SpeedFactor);
                foreach (var booked in windows)
                {
                    if (booked.Start - cursor >= duration) break;   // fits in this gap
                    if (booked.End > cursor) cursor = booked.End;   // pushed past it
                }
                cursor = cursor.Add(duration);                      // serial production per machine
            }

            var promised = DateOnly.FromDateTime(cursor).AddDays(lab.TransitDaysToHub + HubProcessingDays);

            if (best is null || promised < best.PromisedDeliveryDate)
            {
                best = new TrialPlacementResult(
                    promised,
                    lab.Id,
                    machine.Id,
                    $"In xong ước tính {cursor:dd/MM HH:mm} UTC tại {lab.Name} · máy {machine.Name}, " +
                    $"cộng {lab.TransitDaysToHub} ngày vận chuyển về hub + {HubProcessingDays} ngày hub xử lý");
            }
        }

        return best;
    }

    private static bool TryBuildSpec(TrialPlacementLine line, out JobSpecification spec)
    {
        spec = null!;
        try
        {
            var technology = line.MaterialCode.Equals("RESIN", StringComparison.OrdinalIgnoreCase)
                ? PrintTechnology.Sla
                : PrintTechnology.Fdm;

            spec = JobSpecification.Create(
                BuildVolume.Create(line.PartWidthMm, line.PartDepthMm, line.PartHeightMm),
                line.MaterialCode,
                line.ColorCode,
                line.LayerHeightMm,
                line.ToleranceMm,
                technology,
                Math.Max(line.TotalGrams, 0.1m));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
