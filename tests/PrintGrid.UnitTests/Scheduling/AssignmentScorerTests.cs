using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.UnitTests.Scheduling;

public class AssignmentScorerTests
{
    private readonly AssignmentScorer _scorer = new();

    [Fact]
    public void Score_prefers_the_lab_with_more_slack_before_the_due_date()
    {
        var job = JobWithDueDateInDays(7);
        var tight = Placement(reliability: 0.9m, endOffsetHours: 160, utilization: 0.5m, cost: 50m);
        var roomy = Placement(reliability: 0.9m, endOffsetHours: 24, utilization: 0.5m, cost: 50m);

        var ranked = _scorer.Rank(job, new[] { tight, roomy }, maxCostAcrossPlacements: 50m);

        ranked[0].Placement.Should().BeSameAs(roomy);
        ranked[0].Breakdown["due_date_slack"].Should().BeGreaterThan(ranked[1].Breakdown["due_date_slack"]);
    }

    [Fact]
    public void Score_prefers_the_more_reliable_lab_when_timing_is_equal()
    {
        var job = JobWithDueDateInDays(7);
        var reliable = Placement(reliability: 0.98m, endOffsetHours: 48, utilization: 0.5m, cost: 50m);
        var shaky = Placement(reliability: 0.60m, endOffsetHours: 48, utilization: 0.5m, cost: 50m);

        var ranked = _scorer.Rank(job, new[] { shaky, reliable }, maxCostAcrossPlacements: 50m);

        ranked[0].Placement.Should().BeSameAs(reliable);
    }

    [Fact]
    public void Score_prefers_the_less_loaded_machine_when_everything_else_matches()
    {
        var job = JobWithDueDateInDays(7);
        var busy = Placement(reliability: 0.9m, endOffsetHours: 48, utilization: 0.95m, cost: 50m);
        var free = Placement(reliability: 0.9m, endOffsetHours: 48, utilization: 0.10m, cost: 50m);

        var ranked = _scorer.Rank(job, new[] { busy, free }, maxCostAcrossPlacements: 50m);

        ranked[0].Placement.Should().BeSameAs(free);
    }

    [Fact]
    public void Score_stays_within_the_zero_to_one_range()
    {
        var job = JobWithDueDateInDays(7);
        var placement = Placement(reliability: 1.0m, endOffsetHours: 1, utilization: 0m, cost: 0m);

        var scored = _scorer.Score(job, placement, maxCost: 100m);

        scored.Score.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void Weights_sum_to_one()
    {
        ScoringWeights.Default.Total.Should().Be(1.0m);
    }

    private static Job JobWithDueDateInDays(int days) =>
        Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            estimatedPrintMinutes: 240,
            internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days)));

    private static PlacementProposal Placement(
        decimal reliability,
        int endOffsetHours,
        decimal utilization,
        decimal cost)
    {
        var lab = Lab.Onboard("Lab", "Ha Noi", transitDaysToHub: 1);
        lab.UpdatePerformance(reliability, reliability);
        var machine = MachineBuilder.Default(lab.Id);
        lab.AddMachine(machine);

        var start = DateTime.UtcNow.AddHours(1);
        return new PlacementProposal(
            new CapabilityCandidate(lab, machine),
            start,
            start.AddHours(endOffsetHours),
            cost,
            utilization);
    }
}
