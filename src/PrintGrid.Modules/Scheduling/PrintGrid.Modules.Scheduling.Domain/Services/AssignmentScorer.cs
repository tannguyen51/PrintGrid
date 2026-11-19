using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

public record PlacementProposal(
    CapabilityCandidate Candidate,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    decimal EstimatedCost,
    decimal MachineUtilization);

public record ScoredPlacement(PlacementProposal Placement, decimal Score, IReadOnlyDictionary<string, decimal> Breakdown);

public class AssignmentScorer(ScoringWeights? weights = null)
{
    private readonly ScoringWeights _weights = weights ?? ScoringWeights.Default;

    public IReadOnlyList<ScoredPlacement> Rank(
        Job job,
        IEnumerable<PlacementProposal> placements,
        decimal maxCostAcrossPlacements,
        IReadOnlyDictionary<Guid, LabScoreSnapshot>? labScores = null)
    {
        return
        [
            .. placements
                .Select(p => Score(job, p, maxCostAcrossPlacements, labScores))
                .OrderByDescending(s => s.Score)
        ];
    }

    public ScoredPlacement Score(Job job, PlacementProposal placement, decimal maxCost, IReadOnlyDictionary<Guid, LabScoreSnapshot>? labScores = null)
    {
        var dueUtc = job.InternalDueDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var slackHours = (decimal)(dueUtc - placement.PlannedEndUtc).TotalHours;
        var slackScore = Normalize(slackHours, 0m, 168m);

        var lab = placement.Candidate.Lab;
        var score = labScores?.TryGetValue(lab.Id, out var labScore) == true ? labScore : null;

        var onTimeScore = score?.OnTimeRate ?? lab.OnTimeDeliveryRate;
        var qualityScore = score?.FirstPassYield ?? lab.FirstPassYield;
        var loadScore = 1m - Math.Clamp(placement.MachineUtilization, 0m, 1m);
        var costScore = maxCost <= 0 ? 1m : 1m - Math.Clamp(placement.EstimatedCost / maxCost, 0m, 1m);
        var transitScore = score?.TransitRate ?? Normalize(5m - lab.TransitDaysToHub, 0m, 5m);

        var breakdown = new Dictionary<string, decimal>
        {
            ["due_date_slack"] = slackScore,
            ["on_time_rate"] = onTimeScore,
            ["first_pass_yield"] = qualityScore,
            ["load_balance"] = loadScore,
            ["cost"] = costScore,
            ["transit"] = transitScore
        };

        var totalScore =
            slackScore * _weights.DueDateSlack
            + onTimeScore * _weights.OnTimeRate
            + qualityScore * _weights.FirstPassYield
            + loadScore * _weights.LoadBalance
            + costScore * _weights.Cost
            + transitScore * _weights.Transit;

        return new ScoredPlacement(placement, decimal.Round(totalScore, 4), breakdown);
    }

    private static decimal Normalize(decimal value, decimal min, decimal max) => max <= min ? 0m : Math.Clamp((value - min) / (max - min), 0m, 1m);
}
