namespace PrintGrid.Modules.Scheduling.Domain.Services;

public class ScoringWeights
{
    public decimal DueDateSlack { get; init; } = 0.35m;
    public decimal LabReliability { get; init; } = 0.25m;
    public decimal LoadBalance { get; init; } = 0.20m;
    public decimal Cost { get; init; } = 0.10m;
    public decimal Transit { get; init; } = 0.10m;

    public static ScoringWeights Default => new();

    public decimal Total => DueDateSlack + LabReliability + LoadBalance + Cost + Transit;
}
