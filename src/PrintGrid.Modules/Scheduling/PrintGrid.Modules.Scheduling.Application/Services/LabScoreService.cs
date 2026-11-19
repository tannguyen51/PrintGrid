using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Application.Services;

public interface ILabScoreService
{
    Task<IReadOnlyDictionary<Guid, LabScoreSnapshot>> ComputeScoresAsync(
        IReadOnlyList<Guid> labIds,
        LabScoreParameters parameters,
        CancellationToken cancellationToken = default);
}

public interface ILabScoreParameterProvider
{
    Task<LabScoreParameters> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class LabScoreService(IJobRepository jobs, ILabRepository labs, IDateTimeProvider clock) : ILabScoreService
{
    public async Task<IReadOnlyDictionary<Guid, LabScoreSnapshot>> ComputeScoresAsync(
        IReadOnlyList<Guid> labIds,
        LabScoreParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, LabScoreSnapshot>();
        foreach (var labId in labIds)
        {
            var lab = await labs.GetByIdAsync(labId, cancellationToken);
            if (lab is null) continue;
            var labJobs = await jobs.GetByLabIdAsync(labId, cancellationToken);
            var metrics = LabMetricsCalculator.ComputeForLab(lab, labJobs, clock.UtcNow, useStoredFallback: true);
            result[labId] = Compute(labId, lab.TransitDaysToHub, metrics, parameters);
        }
        return result;
    }

    private static LabScoreSnapshot Compute(Guid labId, int transitDaysToHub, LabMetricsSnapshot metrics, LabScoreParameters parameters)
    {
        var completed = metrics.CompletedCount;
        var provisional = LabScoreCalculator.IsProvisional(completed, parameters.ProvisionalThreshold);

        var onTimeRaw = metrics.OnTimeDeliveryRate;
        var qualityRaw = metrics.FirstPassYield;

        var onTime = provisional
            ? LabScoreCalculator.SmoothedRate(onTimeRaw, parameters.ProvisionalBaseScore, completed, parameters.ProvisionalWeightK)
            : onTimeRaw;
        var quality = provisional
            ? LabScoreCalculator.SmoothedRate(qualityRaw, parameters.ProvisionalBaseScore, completed, parameters.ProvisionalWeightK)
            : qualityRaw;

        // Cost and transit are normalized later by the scorer; here we just expose raw normalized views.
        var costRate = 1m; // placeholder: actual cost comparison happens per-proposal
        var transitRate = Math.Clamp(1m - transitDaysToHub / 5m, 0m, 1m);

        return new LabScoreSnapshot(labId, onTime, quality, metrics.UtilizationRate, costRate, transitRate, provisional, completed);
    }
}
