namespace PrintGrid.Modules.Scheduling.Application.Services;

public sealed record TransportAllowanceQuote(
    string Zone,
    string WeightClass,
    decimal TotalWeightGrams,
    decimal ZoneRate,
    decimal WeightMultiplier,
    decimal Allowance,
    string Currency = "VND");

public sealed class TransportAllowancePolicy(
    IReadOnlyDictionary<string, decimal> zoneRates,
    decimal mediumMultiplier,
    decimal largeMultiplier)
{
    public TransportAllowanceQuote Quote(string zone, decimal totalWeightGrams)
    {
        if (totalWeightGrams <= 0) throw new ArgumentOutOfRangeException(nameof(totalWeightGrams));
        var normalizedZone = zone.Trim().ToUpperInvariant();
        if (!zoneRates.TryGetValue(normalizedZone, out var rate))
            throw new InvalidOperationException($"Transport zone '{normalizedZone}' has no configured rate");

        var (weightClass, multiplier) = totalWeightGrams switch
        {
            <= 1000m => ("UP_TO_1_KG", 1m),
            <= 5000m => ("UP_TO_5_KG", mediumMultiplier),
            _ => ("OVER_5_KG", largeMultiplier)
        };
        return new TransportAllowanceQuote(
            normalizedZone,
            weightClass,
            decimal.Round(totalWeightGrams, 2),
            rate,
            multiplier,
            decimal.Round(rate * multiplier, 0));
    }
}
