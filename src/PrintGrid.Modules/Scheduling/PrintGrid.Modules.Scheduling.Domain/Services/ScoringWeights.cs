namespace PrintGrid.Modules.Scheduling.Domain.Services;

public class ScoringWeights
{
    public decimal DueDateSlack { get; init; } = 0.30m;
    public decimal OnTimeRate { get; init; } = 0.15m;
    public decimal FirstPassYield { get; init; } = 0.10m;
    public decimal LoadBalance { get; init; } = 0.20m;
    public decimal Cost { get; init; } = 0.10m;
    public decimal Transit { get; init; } = 0.15m;

    public static ScoringWeights Default => new();

    public decimal Total => DueDateSlack + OnTimeRate + FirstPassYield + LoadBalance + Cost + Transit;
}
