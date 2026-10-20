using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>An immutable scoring period. Activating a new period never rewrites prior weights.</summary>
public sealed class ScoringWeightConfiguration
{
    private ScoringWeightConfiguration() { }

    public Guid Id { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public decimal DueDateSlack { get; private set; }
    public decimal LabReliability { get; private set; }
    public decimal LoadBalance { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Transit { get; private set; }
    public bool IsActive { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid ChangedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public ScoringWeights ToWeights() => new()
    {
        DueDateSlack = DueDateSlack,
        LabReliability = LabReliability,
        LoadBalance = LoadBalance,
        Cost = Cost,
        Transit = Transit
    };

    public ScoringParameterSet ToParameterSet() => new(Version, ToWeights());

    public static Result<ScoringWeightConfiguration> Create(
        string version,
        ScoringWeights weights,
        string reason,
        Guid changedBy,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Trim().Length > 64)
            return Result.Failure<ScoringWeightConfiguration>(Error.Validation("Version is required and must not exceed 64 characters"));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            return Result.Failure<ScoringWeightConfiguration>(Error.Validation("Change reason must contain at least 10 characters"));
        if (weights.DueDateSlack < 0 || weights.LabReliability < 0 || weights.LoadBalance < 0 || weights.Cost < 0 || weights.Transit < 0)
            return Result.Failure<ScoringWeightConfiguration>(Error.Validation("Scoring weights cannot be negative"));
        if (weights.Total != 1.0m)
            return Result.Failure<ScoringWeightConfiguration>(Error.Validation("Scoring weights must total exactly 1.0"));

        return Result.Success(new ScoringWeightConfiguration
        {
            Id = Guid.NewGuid(),
            Version = version.Trim(),
            DueDateSlack = weights.DueDateSlack,
            LabReliability = weights.LabReliability,
            LoadBalance = weights.LoadBalance,
            Cost = weights.Cost,
            Transit = weights.Transit,
            IsActive = true,
            Reason = reason.Trim(),
            ChangedBy = changedBy,
            CreatedAtUtc = createdAtUtc
        });
    }

    public void Deactivate() => IsActive = false;
}
