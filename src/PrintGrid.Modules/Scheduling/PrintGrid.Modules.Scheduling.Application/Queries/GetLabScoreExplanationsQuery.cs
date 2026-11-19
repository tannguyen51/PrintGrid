using MediatR;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.Queries;

public sealed record LabScoreExplanationDto(
    Guid LabId,
    string LabName,
    int CompletedJobs,
    int TotalAssignedJobs,
    decimal OnTimeRate,
    decimal FirstPassYield,
    decimal UtilizationRate,
    bool IsProvisional,
    decimal SmoothedOnTimeRate,
    decimal SmoothedFirstPassYield,
    string ProvisionalLabel,
    int ProvisionalThreshold);

public sealed record GetLabScoreExplanationsQuery : IRequest<IReadOnlyList<LabScoreExplanationDto>>;

public sealed class GetLabScoreExplanationsQueryHandler(
    ILabRepository labs,
    IJobRepository jobs,
    ILabScoreParameterProvider parameterProvider,
    IDateTimeProvider clock)
    : IRequestHandler<GetLabScoreExplanationsQuery, IReadOnlyList<LabScoreExplanationDto>>
{
    public async Task<IReadOnlyList<LabScoreExplanationDto>> Handle(GetLabScoreExplanationsQuery request, CancellationToken cancellationToken)
    {
        var activeLabs = await labs.GetActiveWithMachinesAsync(cancellationToken);
        var parameters = await parameterProvider.GetAsync(cancellationToken);
        var result = new List<LabScoreExplanationDto>();

        foreach (var lab in activeLabs)
        {
            var labJobs = await jobs.GetByLabIdAsync(lab.Id, cancellationToken);
            var metrics = LabMetricsCalculator.ComputeForLab(lab, labJobs, clock.UtcNow, useStoredFallback: true);
            var provisional = LabScoreCalculator.IsProvisional(metrics.CompletedCount, parameters.ProvisionalThreshold);
            var smoothedOnTime = provisional
                ? LabScoreCalculator.SmoothedRate(metrics.OnTimeDeliveryRate, parameters.ProvisionalBaseScore, metrics.CompletedCount, parameters.ProvisionalWeightK)
                : metrics.OnTimeDeliveryRate;
            var smoothedQuality = provisional
                ? LabScoreCalculator.SmoothedRate(metrics.FirstPassYield, parameters.ProvisionalBaseScore, metrics.CompletedCount, parameters.ProvisionalWeightK)
                : metrics.FirstPassYield;

            result.Add(new LabScoreExplanationDto(
                lab.Id,
                lab.Name,
                metrics.CompletedCount,
                metrics.TotalAssignedCount,
                metrics.OnTimeDeliveryRate,
                metrics.FirstPassYield,
                metrics.UtilizationRate,
                provisional,
                smoothedOnTime,
                smoothedQuality,
                provisional ? "Tạm thi" : "Chính thức",
                parameters.ProvisionalThreshold));
        }

        return result;
    }
}
