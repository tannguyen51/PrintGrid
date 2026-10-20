using MediatR;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Services;
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
    private static readonly JobStatus[] _activeTimelineStatuses =
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
                        && _activeTimelineStatuses.Contains(j.Status)
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
        var targetStats = LabMetricsCalculator.ComputeForLab(lab, targetJobs, now);

        var benchmarkLabs = allLabs
            .Where(l => l.IsActive && l.Status == LabStatus.Active)
            .ToList();
        if (benchmarkLabs.Count == 0)
            benchmarkLabs.Add(lab);

        var benchmarkStats = benchmarkLabs
            .Select(l => LabMetricsCalculator.ComputeForLab(l, jobsByLab.GetValueOrDefault(l.Id) ?? Array.Empty<Job>(), now))
            .ToList();

        var networkAvg = new LabPerformanceMetricsDto(
            OnTimeDeliveryRate: decimal.Round(benchmarkStats.Average(s => s.OnTimeDeliveryRate), 4),
            FirstPassYield: decimal.Round(benchmarkStats.Average(s => s.FirstPassYield), 4),
            AcceptanceRate: decimal.Round(benchmarkStats.Average(s => s.AcceptanceRate), 4),
            UtilizationRate: decimal.Round(benchmarkStats.Average(s => s.UtilizationRate), 4));

        var networkAvgStanding = decimal.Round(benchmarkStats.Average(s => s.IsProvisional ? 0.70m : Standing(s)), 4);

        var targetStanding = targetStats.IsProvisional ? 0.70m : Standing(targetStats);
        var targetLabel = targetStats.IsProvisional
            ? "Điểm tạm (< 10 job)"
            : StandingLabel(targetStanding);

        return Result.Success(new LabPerformanceDto(
            LabId: lab.Id,
            LabName: lab.Name,
            CompletedJobsCount: targetStats.CompletedCount,
            TotalAssignedJobsCount: targetStats.TotalAssignedCount,
            IsProvisional: targetStats.IsProvisional,
            StandingScore: targetStanding,
            StandingLabel: targetLabel,
            LabMetrics: new LabPerformanceMetricsDto(
                targetStats.OnTimeDeliveryRate,
                targetStats.FirstPassYield,
                targetStats.AcceptanceRate,
                targetStats.UtilizationRate),
            NetworkAnonymousAverage: networkAvg,
            NetworkAverageStandingScore: networkAvgStanding,
            ActiveLabsInBenchmark: benchmarkLabs.Count));
    }

    private static decimal Standing(LabMetricsSnapshot metrics) =>
        decimal.Round(
            Math.Clamp(
                0.35m * metrics.OnTimeDeliveryRate
                + 0.30m * metrics.FirstPassYield
                + 0.15m * metrics.AcceptanceRate
                + 0.10m * metrics.UtilizationRate
                + 0.10m,
                0m, 1m),
            4);

    private static string StandingLabel(decimal standingScore) =>
        standingScore >= 0.85m
            ? "Xuất sắc"
            : standingScore >= 0.70m
                ? "Tốt"
                : standingScore >= 0.50m
                    ? "Đạt"
                    : "Cần cải thiện";
}
