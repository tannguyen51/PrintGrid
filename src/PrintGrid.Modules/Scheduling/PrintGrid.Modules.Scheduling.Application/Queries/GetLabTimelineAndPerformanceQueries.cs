using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public record GetLabTimelineQuery(Guid LabId) : IRequest<Result<LabTimelineDto>>;

public record GetLabPerformanceQuery(Guid LabId) : IRequest<Result<LabPerformanceDto>>;

public class GetLabTimelineAndPerformanceQueryHandler :
    IRequestHandler<GetLabTimelineQuery, Result<LabTimelineDto>>,
    IRequestHandler<GetLabPerformanceQuery, Result<LabPerformanceDto>>
{
    private static readonly JobStatus[] ActiveTimelineStatuses =
    {
        JobStatus.Assigned,
        JobStatus.Accepted,
        JobStatus.InProgress
    };

    private readonly ILabRepository _labs;
    private readonly IJobRepository _jobs;
    private readonly IOrderRepository _orders;
    private readonly IModelRepository _models;

    public GetLabTimelineAndPerformanceQueryHandler(
        ILabRepository labs,
        IJobRepository jobs,
        IOrderRepository orders,
        IModelRepository models)
    {
        _labs = labs;
        _jobs = jobs;
        _orders = orders;
        _models = models;
    }

    public async Task<Result<LabTimelineDto>> Handle(GetLabTimelineQuery query, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<LabTimelineDto>(Error.NotFound("Lab", query.LabId));

        var labJobs = await _jobs.GetByLabIdAsync(lab.Id, cancellationToken);

        // AC01 (FR-LAB-006): Only active jobs (Assigned, Accepted, InProgress) occupy future timeline slots.
        // As soon as a job is completed (transitions to AwaitingInspection / Completed), its slot disappears from future timeline.
        var activeJobs = labJobs
            .Where(j => j.MachineId.HasValue
                        && ActiveTimelineStatuses.Contains(j.Status)
                        && j.PlannedStartUtc.HasValue
                        && j.PlannedEndUtc.HasValue)
            .OrderBy(j => j.PlannedStartUtc)
            .ToList();

        var orderMap = new Dictionary<Guid, string?>();
        foreach (var itemId in activeJobs.Select(j => j.OrderItemId).Distinct())
        {
            var order = await _orders.GetByItemIdAsync(itemId, cancellationToken);
            orderMap[itemId] = order?.OrderNumber;
        }

        var modelMap = new Dictionary<Guid, string?>();
        foreach (var modelId in activeJobs.Select(j => j.ModelId).Distinct())
        {
            var model = await _models.GetByIdAsync(modelId, cancellationToken);
            modelMap[modelId] = model?.FileName;
        }

        var machineTimelines = lab.Machines
            .OrderBy(m => m.Name)
            .Select(machine =>
            {
                var slots = activeJobs
                    .Where(j => j.MachineId == machine.Id)
                    .Select(j => new MachineTimelineSlotDto(
                        j.Id,
                        orderMap.GetValueOrDefault(j.OrderItemId),
                        modelMap.GetValueOrDefault(j.ModelId),
                        j.Specification.MaterialCode,
                        j.Specification.ColorCode,
                        j.Status.ToString(),
                        j.AttemptNumber,
                        j.InternalDueDate,
                        j.EstimatedPrintMinutes,
                        j.PlannedStartUtc!.Value,
                        j.PlannedEndUtc!.Value))
                    .ToList();

                var totalMinutes = slots.Sum(s =>
                    (int)Math.Max(0, (s.PlannedEndUtc - s.PlannedStartUtc).TotalMinutes));

                return new MachineTimelineDto(
                    machine.Id,
                    machine.Name,
                    machine.Model,
                    machine.Technology,
                    machine.Status,
                    machine.MaintenanceFromUtc,
                    machine.MaintenanceUntilUtc,
                    machine.MaintenanceReason,
                    totalMinutes,
                    slots);
            })
            .ToList();

        return Result.Success(new LabTimelineDto(
            lab.Id,
            lab.Name,
            lab.OperatingHours,
            lab.TransitDaysToHub,
            DateTime.UtcNow,
            machineTimelines));
    }

    public async Task<Result<LabPerformanceDto>> Handle(GetLabPerformanceQuery query, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(query.LabId, cancellationToken);
        if (lab is null)
            return Result.Failure<LabPerformanceDto>(Error.NotFound("Lab", query.LabId));

        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(-90);

        var allLabs = await _labs.GetAllAsync(cancellationToken);
        var recentJobs = await _jobs.GetAllAssignedToLabsAsync(windowStart, cancellationToken);
        var jobsByLab = recentJobs
            .Where(j => j.LabId.HasValue)
            .GroupBy(j => j.LabId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Job>)g.ToList());

        var targetJobs = jobsByLab.GetValueOrDefault(lab.Id) ?? Array.Empty<Job>();
        var targetStats = ComputeForLab(lab, targetJobs, now);

        var benchmarkLabs = allLabs
            .Where(l => l.IsActive && l.Status == LabStatus.Active)
            .ToList();
        if (benchmarkLabs.Count == 0)
            benchmarkLabs.Add(lab);

        var benchmarkStats = benchmarkLabs
            .Select(l => ComputeForLab(l, jobsByLab.GetValueOrDefault(l.Id) ?? Array.Empty<Job>(), now))
            .ToList();

        var networkAvg = new LabPerformanceMetricsDto(
            OnTimeDeliveryRate: decimal.Round(benchmarkStats.Average(s => s.Metrics.OnTimeDeliveryRate), 4),
            FirstPassYield: decimal.Round(benchmarkStats.Average(s => s.Metrics.FirstPassYield), 4),
            AcceptanceRate: decimal.Round(benchmarkStats.Average(s => s.Metrics.AcceptanceRate), 4),
            UtilizationRate: decimal.Round(benchmarkStats.Average(s => s.Metrics.UtilizationRate), 4));

        var networkAvgStanding = decimal.Round(benchmarkStats.Average(s => s.StandingScore), 4);

        return Result.Success(new LabPerformanceDto(
            LabId: lab.Id,
            LabName: lab.Name,
            CompletedJobsCount: targetStats.CompletedCount,
            TotalAssignedJobsCount: targetStats.TotalAssignedCount,
            IsProvisional: targetStats.IsProvisional,
            StandingScore: targetStats.StandingScore,
            StandingLabel: targetStats.StandingLabel,
            LabMetrics: targetStats.Metrics,
            NetworkAnonymousAverage: networkAvg,
            NetworkAverageStandingScore: networkAvgStanding,
            ActiveLabsInBenchmark: benchmarkLabs.Count));
    }

    private static (
        int CompletedCount,
        int TotalAssignedCount,
        bool IsProvisional,
        decimal StandingScore,
        string StandingLabel,
        LabPerformanceMetricsDto Metrics) ComputeForLab(Lab lab, IReadOnlyList<Job> labJobs, DateTime nowUtc)
    {
        var completedJobs = labJobs
            .Where(j => j.Status is JobStatus.AwaitingInspection or JobStatus.Completed || j.CompletedAtUtc.HasValue)
            .ToList();

        var completedCount = completedJobs.Count;
        var totalAssignedCount = labJobs.Count;

        decimal odr;
        if (completedCount > 0)
        {
            var onTime = completedJobs.Count(j =>
                DateOnly.FromDateTime(j.CompletedAtUtc ?? nowUtc) <= j.InternalDueDate);
            odr = decimal.Round((decimal)onTime / completedCount, 4);
        }
        else
        {
            odr = decimal.Round(lab.OnTimeDeliveryRate, 4);
        }

        decimal fpy;
        if (completedCount > 0)
        {
            var passedFirstAttempt = completedJobs.Count(j =>
                j.AttemptNumber == 1
                && j.Status != JobStatus.Failed
                && j.QcProofStatus != QcProofStatus.Rejected);
            fpy = decimal.Round((decimal)passedFirstAttempt / completedCount, 4);
        }
        else
        {
            fpy = decimal.Round(lab.FirstPassYield, 4);
        }

        decimal acceptanceRate;
        if (totalAssignedCount > 0)
        {
            var acceptedCount = labJobs.Count(j =>
                j.Status is JobStatus.Accepted or JobStatus.InProgress or JobStatus.AwaitingInspection or JobStatus.Completed
                || j.StartedAtUtc.HasValue
                || j.CompletedAtUtc.HasValue);
            acceptanceRate = decimal.Round((decimal)acceptedCount / totalAssignedCount, 4);
        }
        else
        {
            acceptanceRate = 1.0m;
        }

        var activeMachinesCount = Math.Max(1, lab.Machines.Count(m => m.Status != MachineStatus.Offline));
        var recentMinutes = labJobs
            .Where(j => j.CreatedAt >= nowUtc.AddDays(-7))
            .Sum(j => j.ActualPrintMinutes ?? j.EstimatedPrintMinutes);
        var capacityMinutes7d = activeMachinesCount * 7m * 8m * 60m;
        var utilizationRate = capacityMinutes7d > 0
            ? decimal.Round(Math.Clamp(recentMinutes / capacityMinutes7d, 0m, 1m), 4)
            : 0m;

        // BR-PERF-002: < 10 completed jobs => provisional score 0.70 (điểm tạm)
        var isProvisional = completedCount < 10;
        var compositeScore = decimal.Round(
            Math.Clamp(0.35m * odr + 0.30m * fpy + 0.15m * acceptanceRate + 0.10m * utilizationRate + 0.10m, 0m, 1m),
            4);
        var standingScore = isProvisional ? 0.70m : compositeScore;

        var standingLabel = isProvisional
            ? "Điểm tạm (< 10 job)"
            : standingScore >= 0.85m
                ? "Xuất sắc"
                : standingScore >= 0.70m
                    ? "Tốt"
                    : standingScore >= 0.50m
                        ? "Đạt"
                        : "Cần cải thiện";

        var metrics = new LabPerformanceMetricsDto(odr, fpy, acceptanceRate, utilizationRate);
        return (completedCount, totalAssignedCount, isProvisional, standingScore, standingLabel, metrics);
    }
}
