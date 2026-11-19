namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>
/// Provisional-score math for new labs: a weighted blend of a controlled baseline and the
/// lab's actual operational history. Avoids both "excluded for no history" and "perfect by default".
/// </summary>
public static class LabScoreCalculator
{
    /// <summary>
    /// Smooths an observed rate with a controlled baseline while the lab has little history.
    /// <paramref name="k"/> is the weight of the baseline in "equivalent job count".
    /// <paramref name="n"/> is the number of completed jobs observed.
    /// </summary>
    public static decimal SmoothedRate(decimal actualRate, decimal baselineRate, int n, int k)
    {
        if (k < 0) throw new ArgumentOutOfRangeException(nameof(k), "k must be non-negative");
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "n must be non-negative");
        if (k == 0 && n == 0) return baselineRate;
        return (k * baselineRate + n * actualRate) / (k + n);
    }

    /// <summary>A lab is considered provisional until it has completed enough jobs to have a reliable rate.</summary>
    public static bool IsProvisional(int completedJobs, int threshold) => completedJobs < threshold;
}
