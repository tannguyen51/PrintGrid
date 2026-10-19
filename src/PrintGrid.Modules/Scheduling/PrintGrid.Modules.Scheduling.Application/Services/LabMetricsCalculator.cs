using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Application.Services;

/// <summary>
/// The BR-PERF-001 sliding-window math shared by the lab performance ledger
/// (FR-ANAL-002) and the network SLA report (FR-ANAL-003).
///
/// Window filtering is the caller's job: it passes the jobs already restricted to the
/// rolling window. "Completed" keeps the ledger's existing definition — the lab finished
/// printing (<see cref="JobStatus.AwaitingInspection"/> or <see cref="JobStatus.Completed"/>,
/// or a completion timestamp exists) — so both surfaces report the same numbers.
/// </summary>
public static class LabMetricsCalculator
{
    /// <summary>BR-PERF-002: below ten completed jobs a standing score is provisional.</summary>
    public const int MinimumScoredJobs = 10;

    public static LabMetricsSnapshot ComputeForLab(
        Lab lab,
        IReadOnlyList<Job> labJobs,
        DateTime nowUtc,
        bool useStoredFallback = true)
    {
        var completedJobs = CompletedJobs(labJobs);
        var activeMachines = Math.Max(1, lab.Machines.Count(m => m.Status != MachineStatus.Offline));
        var recentMinutes = labJobs
            .Where(j => j.CreatedAt >= nowUtc.AddDays(-7))
            .Sum(j => j.ActualPrintMinutes ?? j.EstimatedPrintMinutes);

        var stored = useStoredFallback
            ? new StoredRates(lab.OnTimeDeliveryRate, lab.FirstPassYield)
            : new StoredRates(null, null);

        return Compute(labJobs, completedJobs, activeMachines, recentMinutes, stored, useStoredFallback, nowUtc);
    }

    /// <summary>
    /// Network aggregate computed from the underlying jobs rather than averaging lab rates,
    /// so a lab with 100 jobs weighs more than one with two. <paramref name="labs"/> supplies
    /// the machine capacity used for utilization.
    /// </summary>
    public static LabMetricsSnapshot ComputeNetwork(
        IReadOnlyList<Lab> labs,
        IReadOnlyList<Job> jobs,
        DateTime nowUtc)
    {
        var completedJobs = CompletedJobs(jobs);
        var activeMachines = labs.Sum(l => Math.Max(1, l.Machines.Count(m => m.Status != MachineStatus.Offline)));
        var recentMinutes = jobs
            .Where(j => j.CreatedAt >= nowUtc.AddDays(-7))
            .Sum(j => j.ActualPrintMinutes ?? j.EstimatedPrintMinutes);

        return Compute(jobs, completedJobs, Math.Max(1, activeMachines), recentMinutes, new StoredRates(null, null), false, nowUtc);
    }

    private static List<Job> CompletedJobs(IReadOnlyList<Job> jobs) =>
        jobs.Where(j => j.Status is JobStatus.AwaitingInspection or JobStatus.Completed || j.CompletedAtUtc.HasValue)
            .ToList();

    private static LabMetricsSnapshot Compute(
        IReadOnlyList<Job> assignedJobs,
        IReadOnlyList<Job> completedJobs,
        int activeMachines,
        int recentMinutes,
        StoredRates stored,
        bool useStoredFallback,
        DateTime nowUtc)
    {
        var completedCount = completedJobs.Count;
        var totalAssignedCount = assignedJobs.Count;

        var onTimeCount = completedJobs.Count(j =>
            DateOnly.FromDateTime(j.CompletedAtUtc ?? nowUtc) <= j.InternalDueDate);
        var lateCount = completedCount - onTimeCount;

        var odr = completedCount > 0
            ? decimal.Round((decimal)onTimeCount / completedCount, 4)
            : decimal.Round(stored.OnTimeDeliveryRate ?? 0m, 4);

        var firstAttemptPasses = completedJobs.Count(j =>
            j.AttemptNumber == 1
            && j.Status != JobStatus.Failed
            && j.QcProofStatus != QcProofStatus.Rejected);
        var fpy = completedCount > 0
            ? decimal.Round((decimal)firstAttemptPasses / completedCount, 4)
            : decimal.Round(stored.FirstPassYield ?? 0m, 4);

        var acceptedCount = assignedJobs.Count(j =>
            j.Status is JobStatus.Accepted or JobStatus.InProgress or JobStatus.AwaitingInspection or JobStatus.Completed
            || j.StartedAtUtc.HasValue
            || j.CompletedAtUtc.HasValue);
        var acceptanceRate = totalAssignedCount > 0
            ? decimal.Round((decimal)acceptedCount / totalAssignedCount, 4)
            : useStoredFallback ? 1.0m : 0m;

        var capacityMinutes7d = activeMachines * 7m * 8m * 60m;
        var utilizationRate = capacityMinutes7d > 0
            ? decimal.Round(Math.Clamp(recentMinutes / capacityMinutes7d, 0m, 1m), 4)
            : 0m;

        return new LabMetricsSnapshot(
            completedCount,
            totalAssignedCount,
            lateCount,
            odr,
            fpy,
            acceptanceRate,
            utilizationRate,
            IsProvisional: completedCount < MinimumScoredJobs);
    }

    private readonly record struct StoredRates(decimal? OnTimeDeliveryRate, decimal? FirstPassYield);
}

public sealed record LabMetricsSnapshot(
    int CompletedCount,
    int TotalAssignedCount,
    int LateCount,
    decimal OnTimeDeliveryRate,
    decimal FirstPassYield,
    decimal AcceptanceRate,
    decimal UtilizationRate,
    bool IsProvisional);
