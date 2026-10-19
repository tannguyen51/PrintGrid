using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>
/// Pure calibration math for FR-SCHED-008 / BR-ESTIM-002..006. Kept in the domain so the
/// nightly job, the ops endpoint and unit tests all use exactly the same rules.
///
/// Outlier policy:
///  1. BR-ESTIM-003 hard bounds — a sample whose ratio is below 0.3x or above 3.0x the
///     estimate is discarded outright (print failures and operator mistakes).
///  2. Statistical filter — among the remaining samples, drop ratios outside mean ± 2σ.
/// A factor is only produced when at least <see cref="MinimumSamples"/> valid samples remain
/// (BR-ESTIM-006). The factor is mean(Actual / Estimated), clamped to the same 0.5..2.0 range
/// the scheduler already allows for <see cref="Machine.SpeedFactor"/>.
/// </summary>
public static class EstimationCalibration
{
    /// <summary>BR-ESTIM-006: below this many valid samples the previous factor is retained.</summary>
    public const int MinimumSamples = 10;

    public const decimal MinFactor = 0.5m;
    public const decimal MaxFactor = 2.0m;

    /// <summary>BR-ESTIM-003 (configurable in FR-ADMIN-002 later).</summary>
    public const decimal HardMinRatio = 0.3m;
    public const decimal HardMaxRatio = 3.0m;

    /// <summary>BR-ESTIM-005: MAPE above 25 % is the alert threshold.</summary>
    public const decimal MapeAlertThreshold = 0.25m;

    /// <summary>
    /// Applies the hard bounds and then the mean ± 2σ filter. Order is meaningful: σ is
    /// computed after the hard bounds so a single failed print cannot widen the window.
    /// </summary>
    public static IReadOnlyList<CalibrationSample> FilterOutliers(IEnumerable<CalibrationSample> samples)
    {
        var withinHardBounds = samples
            .Where(s => s.Ratio >= HardMinRatio && s.Ratio <= HardMaxRatio)
            .ToList();

        if (withinHardBounds.Count <= 2) return withinHardBounds;

        var mean = (double)withinHardBounds.Average(s => s.Ratio);
        var variance = withinHardBounds
            .Average(s => Math.Pow((double)s.Ratio - mean, 2));
        var standardDeviation = Math.Sqrt(variance);

        // A constant set (σ = 0) has no statistical outliers.
        if (standardDeviation <= 1e-9) return withinHardBounds;

        var lower = mean - 2 * standardDeviation;
        var upper = mean + 2 * standardDeviation;
        return withinHardBounds
            .Where(s => { var ratio = (double)s.Ratio; return ratio >= lower && ratio <= upper; })
            .ToList();
    }

    /// <summary>
    /// Computes the correction factor for a set of raw samples. Returns
    /// <see cref="CalibrationResult.Insufficient"/> (factor 1.0, no change) when fewer than
    /// <see cref="MinimumSamples"/> valid samples remain.
    /// </summary>
    public static CalibrationResult Calculate(IEnumerable<CalibrationSample> samples)
    {
        var valid = FilterOutliers(samples);
        if (valid.Count < MinimumSamples)
            return new CalibrationResult(false, 1.0m, valid.Count, 0m, valid);

        var rawFactor = valid.Average(s => s.Ratio);
        var factor = decimal.Round(Math.Clamp(rawFactor, MinFactor, MaxFactor), 3);

        // MAPE is stored as a 0..1 fraction: mean(|actual − estimated| / estimated).
        var mape = decimal.Round(valid.Average(s => Math.Abs(s.Ratio - 1m)), 4);

        return new CalibrationResult(true, factor, valid.Count, mape, valid);
    }

    /// <summary>Convenience for reporting/status: how many samples survive the outlier rules.</summary>
    public static int CountValid(IEnumerable<CalibrationSample> samples) => FilterOutliers(samples).Count;
}

/// <summary>
/// <see cref="ValidSamples"/> is the count after outlier filtering (the count that gates
/// BR-ESTIM-006), not the raw count.
/// </summary>
public sealed record CalibrationResult(
    bool IsSufficient,
    decimal Factor,
    int ValidSamples,
    decimal Mape,
    IReadOnlyList<CalibrationSample> Samples);
