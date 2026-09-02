using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

public record PlacementProposal(
    CapabilityCandidate Candidate,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    decimal EstimatedCost,
    decimal MachineUtilization);

public record ScoredPlacement(PlacementProposal Placement, decimal Score, IReadOnlyDictionary<string, decimal> Breakdown);

public class AssignmentScorer
{
    private readonly ScoringWeights _weights;

    public AssignmentScorer(ScoringWeights? weights = null) => _weights = weights ?? ScoringWeights.Default;

    public IReadOnlyList<ScoredPlacement> Rank(
        Job job,
        IEnumerable<PlacementProposal> placements,
        decimal maxCostAcrossPlacements)
    {
        return placements
            .Select(p => Score(job, p, maxCostAcrossPlacements))
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    public ScoredPlacement Score(Job job, PlacementProposal placement, decimal maxCost)
    {
        var dueUtc = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var slackHours = (decimal)(dueUtc - placement.PlannedEndUtc).TotalHours;
        var slackScore = Normalize(slackHours, 0m, 168m);

        var reliabilityScore = (placement.Candidate.Lab.OnTimeDeliveryRate + placement.Candidate.Lab.FirstPassYield) / 2m;
        var loadScore = 1m - Math.Clamp(placement.MachineUtilization, 0m, 1m);
        var costScore = maxCost <= 0 ? 1m : 1m - Math.Clamp(placement.EstimatedCost / maxCost, 0m, 1m);
        var transitScore = Normalize(5m - placement.Candidate.Lab.TransitDaysToHub, 0m, 5m);

        var breakdown = new Dictionary<string, decimal>
        {
            ["due_date_slack"] = slackScore,
            ["lab_reliability"] = reliabilityScore,
            ["load_balance"] = loadScore,
            ["cost"] = costScore,
            ["transit"] = transitScore
        };

        var score =
            slackScore * _weights.DueDateSlack
            + reliabilityScore * _weights.LabReliability
            + loadScore * _weights.LoadBalance
            + costScore * _weights.Cost
            + transitScore * _weights.Transit;

        return new ScoredPlacement(placement, decimal.Round(score, 4), breakdown);
    }

    private static decimal Normalize(decimal value, decimal min, decimal max)
    {
        if (max <= min) return 0m;
        return Math.Clamp((value - min) / (max - min), 0m, 1m);
    }
}
