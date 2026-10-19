using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

/// <summary>Persistence for the FR-SCHED-008 calibration pairs.</summary>
public interface ICalibrationSampleRepository
{
    Task<IReadOnlyList<CalibrationSample>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalibrationSample>> GetByMachineIdAsync(Guid machineId, CancellationToken cancellationToken = default);

    /// <summary>Which of the supplied jobs have already been sampled (idempotency by JobId).</summary>
    Task<IReadOnlySet<Guid>> GetSampledJobIdsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<CalibrationSample> samples, CancellationToken cancellationToken = default);
}
