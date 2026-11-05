using MediatR;
using PrintGrid.Modules.Scheduling.Application.DTOs;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

/// <summary>
/// FR-ANAL-002 (lab performance ledger) + FR-ANAL-003 (SLA report) over the BR-PERF-001
/// sliding 90-day window. With <paramref name="LabId"/> set only that lab's row is returned;
/// the network aggregate always covers the whole network.
/// </summary>
public record GetNetworkSlaReportQuery(Guid? LabId = null) : IRequest<Result<SlaReportDto>>;

public class GetNetworkSlaReportQueryHandler(
    ILabRepository labs,
    IJobRepository jobs,
    IDateTimeProvider clock)
    : IRequestHandler<GetNetworkSlaReportQuery, Result<SlaReportDto>>
{
    private const int _windowDays = 90;

    public async Task<Result<SlaReportDto>> Handle(
        GetNetworkSlaReportQuery query,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var windowStart = now.AddDays(-_windowDays);

        var allLabs = await labs.GetAllAsync(cancellationToken);
        if (query.LabId.HasValue && allLabs.All(l => l.Id != query.LabId.Value))
            return Result.Failure<SlaReportDto>(Error.NotFound("Lab", query.LabId.Value));

        var recentJobs = await jobs.GetAllAssignedToLabsAsync(windowStart, cancellationToken);
        var jobsByLab = recentJobs
            .Where(j => j.LabId.HasValue)
            .GroupBy(j => j.LabId!.Value)
            .ToDictionary(g => g.Key, IReadOnlyList<Job> (g) => g.ToList());

        // SLA reporting has no stored fallback: a lab with no completed jobs in the window
        // honestly reports 0 % (BR-PERF-001 "recent behavior"), not its lifetime average.
        var scopedLabs = query.LabId.HasValue
            ? [.. allLabs.Where(l => l.Id == query.LabId.Value)]
            : allLabs;

        var slaLabRowDtos = scopedLabs
            .OrderBy(l => l.Name)
            .Select(lab =>
            {
                var metrics = LabMetricsCalculator.ComputeForLab(
                    lab,
                    jobsByLab.GetValueOrDefault(lab.Id) ?? Array.Empty<Job>(),
                    now,
                    useStoredFallback: false);
                return ToRow(lab.Id, lab.Name, metrics);
            })
            .ToList();

        var networkMetrics = LabMetricsCalculator.ComputeNetwork(allLabs, recentJobs, now);
        var network = ToRow(null, "Toàn mạng lưới", networkMetrics);

        return Result.Success(new SlaReportDto(
            now,
            windowStart,
            now,
            _windowDays,
            query.LabId,
            network,
            slaLabRowDtos));
    }

    private static SlaLabRowDto ToRow(Guid? labId, string labName, LabMetricsSnapshot metrics) =>
        new(
            labId,
            labName,
            metrics.OnTimeDeliveryRate,
            metrics.FirstPassYield,
            metrics.AcceptanceRate,
            metrics.UtilizationRate,
            metrics.CompletedCount,
            metrics.TotalAssignedCount,
            metrics.LateCount,
            metrics.IsProvisional);
}
