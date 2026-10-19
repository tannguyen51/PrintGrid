using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// One (estimated, actual) pair captured from a completed job for FR-SCHED-008 /
/// BR-ESTIM-001. <see cref="JobId"/> is the idempotency key: a job is sampled at most once,
/// so re-running the nightly collector never double-counts the same print.
///
/// BR-ESTIM-002 says calibration happens per machine model. In this codebase a
/// <see cref="MachineId"/> is one physical machine; the report groups by machine and shows
/// the model name, which keeps distinct machines of the same model from masking each other.
/// </summary>
public class CalibrationSample : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public Guid MachineId { get; private set; }
    public Guid ModelId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public decimal EstimatedMinutes { get; private set; }
    public decimal ActualMinutes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Actual / estimated; 1.0 means the slicer was exact.</summary>
    public decimal Ratio => EstimatedMinutes <= 0m ? 1m : ActualMinutes / EstimatedMinutes;

    private CalibrationSample() { }

    public static CalibrationSample Create(
        Guid jobId,
        Guid machineId,
        Guid modelId,
        string materialCode,
        decimal estimatedMinutes,
        decimal actualMinutes,
        DateTime? createdAtUtc = null)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Job id is required", nameof(jobId));
        if (machineId == Guid.Empty) throw new ArgumentException("Machine id is required", nameof(machineId));
        if (estimatedMinutes <= 0m) throw new ArgumentOutOfRangeException(nameof(estimatedMinutes), "Estimated minutes must be positive");
        if (actualMinutes <= 0m) throw new ArgumentOutOfRangeException(nameof(actualMinutes), "Actual minutes must be positive");

        return new CalibrationSample
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            MachineId = machineId,
            ModelId = modelId,
            MaterialCode = string.IsNullOrWhiteSpace(materialCode)
                ? throw new ArgumentException("Material code is required", nameof(materialCode))
                : materialCode.Trim().ToUpperInvariant(),
            EstimatedMinutes = estimatedMinutes,
            ActualMinutes = actualMinutes,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }
}
