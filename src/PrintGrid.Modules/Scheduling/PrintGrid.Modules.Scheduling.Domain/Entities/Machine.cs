using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class Machine : Entity<Guid>
{
    private readonly List<string> _supportedMaterials = new();

    public Guid LabId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public PrintTechnology Technology { get; private set; }
    public BuildVolume BuildVolume { get; private set; } = null!;
    public decimal MinLayerHeightMm { get; private set; }
    public decimal AchievableToleranceMm { get; private set; }
    public MachineStatus Status { get; private set; }
    public decimal SpeedFactor { get; private set; } = 1.0m;

    /// <summary>
    /// FR-SCHED-008 correction factor learned from completed jobs on this machine.
    /// 1.0 means no correction. It is forward-looking only: it is applied to NEW estimates
    /// (quotes/jobs created after it is computed) and never rewrites a job that has already
    /// been promised (BR-ESTIM-004).
    /// </summary>
    public decimal CalibrationFactor { get; private set; } = 1.0m;

    /// <summary>Valid (post-outlier) samples used by the last successful calibration.</summary>
    public int CalibrationSampleCount { get; private set; }

    /// <summary>MAPE of the last successful calibration, as a 0..1 fraction (BR-ESTIM-005).</summary>
    public decimal CalibrationMape { get; private set; }

    public DateTime? CalibrationComputedAtUtc { get; private set; }

    public DateTime? MaintenanceFromUtc { get; private set; }
    public DateTime? MaintenanceUntilUtc { get; private set; }
    public string? MaintenanceReason { get; private set; }

    public IReadOnlyCollection<string> SupportedMaterials => _supportedMaterials.AsReadOnly();

    private Machine() { }

    public static Machine Register(
        Guid labId,
        string name,
        string model,
        PrintTechnology technology,
        BuildVolume buildVolume,
        decimal minLayerHeightMm,
        decimal achievableToleranceMm,
        IEnumerable<string> supportedMaterials)
    {
        var machine = new Machine
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            Name = name.Trim(),
            Model = model.Trim(),
            Technology = technology,
            BuildVolume = buildVolume,
            MinLayerHeightMm = minLayerHeightMm,
            AchievableToleranceMm = achievableToleranceMm,
            Status = MachineStatus.Idle
        };

        machine._supportedMaterials.AddRange(
            supportedMaterials.Select(m => m.Trim().ToUpperInvariant()).Where(m => m.Length > 0).Distinct());

        return machine;
    }

    public void ScheduleMaintenance(DateTime? fromUtc, DateTime? untilUtc, string? reason, bool setMaintenanceNow = false)
    {
        if (fromUtc.HasValue && untilUtc.HasValue && untilUtc.Value <= fromUtc.Value)
            throw new ArgumentException("Maintenance end time must be after start time");

        MaintenanceFromUtc = fromUtc;
        MaintenanceUntilUtc = untilUtc;
        MaintenanceReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        var now = DateTime.UtcNow;
        if (setMaintenanceNow || (fromUtc.HasValue && untilUtc.HasValue && now >= fromUtc.Value && now < untilUtc.Value))
        {
            Status = MachineStatus.Maintenance;
        }
    }

    public void ClearMaintenance()
    {
        MaintenanceFromUtc = null;
        MaintenanceUntilUtc = null;
        MaintenanceReason = null;
        if (Status == MachineStatus.Maintenance)
            Status = MachineStatus.Idle;
    }

    public bool IsInMaintenanceAt(DateTime utcTime)
    {
        if (Status == MachineStatus.Maintenance) return true;
        if (MaintenanceFromUtc.HasValue && MaintenanceUntilUtc.HasValue)
            return utcTime >= MaintenanceFromUtc.Value && utcTime < MaintenanceUntilUtc.Value;
        if (!MaintenanceFromUtc.HasValue && MaintenanceUntilUtc.HasValue)
            return utcTime < MaintenanceUntilUtc.Value;
        return false;
    }

    public bool Supports(JobSpecification spec) =>
        Technology == spec.Technology
        && BuildVolume.CanFit(spec.RequiredVolume)
        && MinLayerHeightMm <= spec.LayerHeightMm
        && AchievableToleranceMm <= spec.ToleranceMm
        && _supportedMaterials.Contains(spec.MaterialCode);

    public void SetStatus(MachineStatus status)
    {
        Status = status;
        if (status == MachineStatus.Idle)
        {
            MaintenanceFromUtc = null;
            MaintenanceUntilUtc = null;
            MaintenanceReason = null;
        }
    }

    public void UpdateSpecifications(
        string name,
        string model,
        PrintTechnology technology,
        BuildVolume buildVolume,
        decimal minLayerHeightMm,
        decimal achievableToleranceMm,
        IEnumerable<string> supportedMaterials)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Machine name and model are required");

        Name = name.Trim();
        Model = model.Trim();
        Technology = technology;
        BuildVolume = buildVolume;
        MinLayerHeightMm = minLayerHeightMm;
        AchievableToleranceMm = achievableToleranceMm;
        _supportedMaterials.Clear();
        _supportedMaterials.AddRange(supportedMaterials
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct());
    }

    public void AdjustSpeedFactor(decimal factor)
    {
        if (factor is < 0.5m or > 2.0m)
            throw new ArgumentOutOfRangeException(nameof(factor), "Speed factor must stay between 0.5 and 2.0");

        SpeedFactor = decimal.Round(factor, 3);
    }

    /// <summary>
    /// Persists a calibration result (FR-SCHED-008). The factor is clamped to 0.5..2.0, the
    /// same guardrail as <see cref="SpeedFactor"/>; a result computed from fewer than ten
    /// valid samples (BR-ESTIM-006) is rejected rather than silently accepted.
    /// </summary>
    public void ApplyCalibrationFactor(decimal factor, int sampleCount, decimal mape, DateTime computedAtUtc)
    {
        if (sampleCount < PrintGrid.Modules.Scheduling.Domain.Services.EstimationCalibration.MinimumSamples)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "At least 10 valid samples are required before a factor can be applied");
        if (mape < 0m)
            throw new ArgumentOutOfRangeException(nameof(mape), "MAPE cannot be negative");

        CalibrationFactor = decimal.Round(Math.Clamp(factor, 0.5m, 2.0m), 3);
        CalibrationSampleCount = sampleCount;
        CalibrationMape = decimal.Round(mape, 4);
        CalibrationComputedAtUtc = computedAtUtc;
    }

    /// <summary>
    /// The forward-application point for BR-ESTIM-004: the raw slicer estimate is multiplied by
    /// <see cref="CalibrationFactor"/> for THIS machine. It is applied by
    /// <c>AssignmentEngine</c> when computing the machine-specific duration, so the stored
    /// <c>Job.EstimatedPrintMinutes</c> stays the untouched baseline that MAPE is measured
    /// against and no existing job is ever rewritten.
    /// </summary>
    public int ApplyToEstimate(int rawEstimatedMinutes)
    {
        if (rawEstimatedMinutes <= 0) return rawEstimatedMinutes;
        return Math.Max(1, (int)Math.Round(rawEstimatedMinutes * CalibrationFactor, MidpointRounding.AwayFromZero));
    }
}
