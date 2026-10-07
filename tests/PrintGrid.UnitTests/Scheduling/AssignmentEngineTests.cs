using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// FR-SCHED-007 (time budget, feasible fallback) and FR-SCHED-009 (the decision log written
/// in the same transaction as the placement it explains).
/// </summary>
public class AssignmentEngineTests
{
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly ILabRepository _labs = Substitute.For<ILabRepository>();
    private readonly IMachineTimelineService _timeline = Substitute.For<IMachineTimelineService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAssignmentDecisionRepository _decisions = Substitute.For<IAssignmentDecisionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTime _now = new(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);

    public AssignmentEngineTests()
    {
        _clock.UtcNow.Returns(_now);
        _unitOfWork.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>()).Returns(true);
        // Every machine is free from "now" and half loaded unless a test says otherwise.
        _timeline.FindEarliestFreeSlotAsync(
                Arg.Any<Machine>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_now.AddHours(1));
        _timeline.GetUtilizationAsync(
                Arg.Any<Machine>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0.5m);
    }

    private AssignmentEngine Engine() => new(
        _jobs,
        _labs,
        new CapabilityFilter(),
        new AssignmentScorer(),
        _timeline,
        _unitOfWork,
        _decisions,
        ScoringParameterSet.Active,
        _clock,
        NullLogger<AssignmentEngine>.Instance);

    private static Job PendingJob(int dueInDays = 5) => Job.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        SpecBuilder.Default(),
        estimatedPrintMinutes: 60,
        internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(dueInDays)));

    private void OneCapableLab()
    {
        var lab = Lab.Onboard("Lab A", "HCM", transitDaysToHub: 1);
        lab.AddMachine(MachineBuilder.Default(lab.Id));
        _labs.GetActiveWithMachinesAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });
    }

    private (Lab First, Lab Second) TwoCapableLabs()
    {
        var first = Lab.Onboard("Lab A", "HCM", 1);
        first.AddMachine(MachineBuilder.Default(first.Id));
        var second = Lab.Onboard("Lab B", "HCM", 1);
        second.AddMachine(MachineBuilder.Default(second.Id));
        _labs.GetActiveWithMachinesAsync(Arg.Any<CancellationToken>()).Returns(new[] { first, second });
        return (first, second);
    }

    [Fact]
    public async Task Assign_does_not_offer_the_job_back_to_the_lab_that_declined_it()
    {
        var (declined, other) = TwoCapableLabs();
        var job = PendingJob();

        var outcome = await Engine().AssignAsync(
            job,
            new AssignmentRequest(AssignmentTriggers.LabDecline, ExcludeLabId: declined.Id));

        outcome.Assigned.Should().BeTrue();
        outcome.Result!.LabId.Should().Be(other.Id,
            "BR-RESCHED-001: repairing a refusal must not hand the job straight back");
    }

    [Fact]
    public async Task Assign_keeps_the_only_capable_lab_even_when_it_is_the_one_that_declined()
    {
        OneCapableLab();
        var job = PendingJob();
        var onlyLabId = (await _labs.GetActiveWithMachinesAsync()).Single().Id;

        var outcome = await Engine().AssignAsync(
            job,
            new AssignmentRequest(AssignmentTriggers.LabDecline, ExcludeLabId: onlyLabId));

        outcome.Assigned.Should().BeTrue(
            "BR-SCHED-006 outranks the exclusion preference: a feasible fallback must still be committed");
        outcome.Result!.LabId.Should().Be(onlyLabId);
    }

    [Fact]
    public async Task Assign_writes_the_decision_log_in_the_same_save_as_the_placement()
    {
        OneCapableLab();
        var job = PendingJob();
        AssignmentDecision? logged = null;
        await _decisions.AddAsync(Arg.Do<AssignmentDecision>(d => logged = d), Arg.Any<CancellationToken>());

        var outcome = await Engine().AssignAsync(
            job, new AssignmentRequest(AssignmentTriggers.InitialAssign, ActorType: "ops"));

        outcome.Assigned.Should().BeTrue();

        // FR-SCHED-009 E1: the entry is added and the placement is committed by ONE save, so a
        // log that cannot be written takes the assignment down with it.
        Received.InOrder(() =>
        {
            _decisions.AddAsync(Arg.Any<AssignmentDecision>(), Arg.Any<CancellationToken>());
            _unitOfWork.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>());
        });

        logged.Should().NotBeNull();
        logged!.JobId.Should().Be(job.Id);
        logged.Trigger.Should().Be(AssignmentTriggers.InitialAssign);
        logged.ActorType.Should().Be("ops");
        logged.Outcome.Should().Be(AssignmentOutcomes.Assigned);
        logged.ChosenMachineId.Should().Be(job.MachineId);
        logged.ChosenScore.Should().BeGreaterThan(0);
        logged.ScoringConfigVersion.Should().Be(ScoringParameterSet.Active.Version,
            "every decision freezes the config version it was made under (BR-CONFIG-003)");
        logged.CandidatesJson.Should().Contain(job.MachineId!.Value.ToString(),
            "the audit must reconstruct the candidate set (BR-ASSIGN-004)");
        logged.RankingJson.Should().Contain("due_date_slack",
            "per-criterion scores are the point of the log (FR-SCHED-009 AC1)");
        logged.TimeBudgetMs.Should().Be(30_000, "BR-SCHED-005 budget is the reschedule hard limit");
    }

    [Fact]
    public async Task Assign_retries_the_next_candidate_when_the_database_rejects_the_first_placement()
    {
        var lab = Lab.Onboard("Lab A", "HCM", 1);
        lab.AddMachine(MachineBuilder.Default(lab.Id));
        lab.AddMachine(MachineBuilder.Default(lab.Id));
        _labs.GetActiveWithMachinesAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });

        var job = PendingJob();
        // The real UnitOfWork detaches every tracked change when a save loses a constraint
        // race, so the engine re-reads a FRESH Pending job — the substitute mirrors that.
        _jobs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => PendingJob());

        // First candidate loses the EXCLUDE-constraint race; the second one commits.
        _unitOfWork.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>()).Returns(false, true);

        var outcome = await Engine().AssignAsync(job, new AssignmentRequest(AssignmentTriggers.InitialAssign));

        outcome.Assigned.Should().BeTrue("the engine must keep trying after a placement race");
        await _decisions.Received(2).AddAsync(Arg.Any<AssignmentDecision>(), Arg.Any<CancellationToken>());
        // In the real UnitOfWork the rejected attempt's row is detached by DiscardPendingChanges,
        // so only the committed attempt's entry survives — the log has one row per decision.
    }

    [Fact]
    public async Task Assign_commits_the_best_ranked_fallback_even_with_no_time_budget_left()
    {
        OneCapableLab();
        var job = PendingJob();

        // BR-SCHED-006: the fallback is attempted before the budget is consulted, so a feasible
        // schedule is never abandoned just because the clock ran out.
        var outcome = await Engine().AssignAsync(
            job,
            new AssignmentRequest(AssignmentTriggers.InitialAssign, TimeBudget: TimeSpan.Zero));

        outcome.Assigned.Should().BeTrue();
        outcome.BudgetExceeded.Should().BeFalse();
        job.Status.Should().Be(JobStatus.Assigned);
    }

    [Fact]
    public async Task Assign_stops_the_search_and_flags_the_budget_when_it_is_spent()
    {
        var lab = Lab.Onboard("Lab A", "HCM", 1);
        lab.AddMachine(MachineBuilder.Default(lab.Id));
        lab.AddMachine(MachineBuilder.Default(lab.Id));
        _labs.GetActiveWithMachinesAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });

        var job = PendingJob();
        _jobs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => PendingJob());
        _unitOfWork.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>()).Returns(false);

        var outcome = await Engine().AssignAsync(
            job,
            new AssignmentRequest(AssignmentTriggers.LabDecline, TimeBudget: TimeSpan.Zero));

        outcome.Assigned.Should().BeFalse();
        outcome.Outcome.Should().Be(AssignmentOutcomes.PlacementRace);
        outcome.BudgetExceeded.Should().BeTrue("BR-SCHED-005 wants the timeout visible in the metrics");
        await _decisions.Received(1).AddAsync(
            Arg.Is<AssignmentDecision>(d => d.Outcome == AssignmentOutcomes.PlacementRace),
            Arg.Any<CancellationToken>());
        // The unplaced outcome is still evidence, so it is committed on its own.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_reports_a_shortfall_when_no_capable_machine_fits_before_the_due_date()
    {
        OneCapableLab();
        var job = PendingJob(dueInDays: 1);
        // The machine cannot start until long past the internal due date.
        _timeline.FindEarliestFreeSlotAsync(
                Arg.Any<Machine>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_now.AddDays(30));

        var outcome = await Engine().AssignAsync(job, new AssignmentRequest(AssignmentTriggers.LabDecline));

        outcome.Assigned.Should().BeFalse();
        outcome.Outcome.Should().Be(AssignmentOutcomes.NoFeasibleSlot);
        outcome.EarliestFeasibleEndUtc.Should().NotBeNull(
            "the date-change flow needs the earliest date the network CAN meet (BR-SCHED-011)");
        job.Status.Should().Be(JobStatus.Pending, "an unplaceable job is left alone, never silently late");
        await _decisions.Received(1).AddAsync(
            Arg.Is<AssignmentDecision>(d => d.Outcome == AssignmentOutcomes.NoFeasibleSlot),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_logs_a_rejection_when_no_lab_can_print_the_specification()
    {
        var lab = Lab.Onboard("Resin only", "HCM", 1);
        lab.AddMachine(MachineBuilder.Default(lab.Id, materials: new[] { "RESIN" }, technology: PrintTechnology.Sla));
        _labs.GetActiveWithMachinesAsync(Arg.Any<CancellationToken>()).Returns(new[] { lab });

        var job = PendingJob();

        var outcome = await Engine().AssignAsync(job, new AssignmentRequest(AssignmentTriggers.InitialAssign));

        outcome.Outcome.Should().Be(AssignmentOutcomes.NoCapableMachine);
        outcome.Rejections.Should().NotBeEmpty();
        await _decisions.Received(1).AddAsync(
            Arg.Is<AssignmentDecision>(d =>
                d.Outcome == AssignmentOutcomes.NoCapableMachine &&
                d.CandidatesJson.Contains("rejected")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_probe_answers_with_the_earliest_feasible_date_without_touching_the_schedule()
    {
        OneCapableLab();
        var job = PendingJob(dueInDays: 1);
        var earliest = _now.AddDays(4);
        _timeline.FindEarliestFreeSlotAsync(
                Arg.Any<Machine>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(earliest);

        var outcome = await Engine().AssignAsync(
            job,
            new AssignmentRequest(
                AssignmentTriggers.DateChangeProbe, IgnoreDueDate: true, ProbeOnly: true));

        outcome.Outcome.Should().Be(AssignmentOutcomes.Probe);
        outcome.EarliestFeasibleEndUtc.Should().NotBeNull();
        job.Status.Should().Be(JobStatus.Pending,
            "a probe must never place work past a committed date behind the customer's back");
        await _decisions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Assign_refuses_a_job_that_is_already_printing()
    {
        OneCapableLab();
        var job = PendingJob();
        var start = _now;
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), start, start.AddHours(1), 0.9m);
        job.Accept(_now);
        job.Start(_now);

        var outcome = await Engine().AssignAsync(job, new AssignmentRequest(AssignmentTriggers.OpsOverride));

        outcome.Assigned.Should().BeFalse();
        outcome.Outcome.Should().Be(AssignmentOutcomes.RejectedByAggregate);
        job.Status.Should().Be(JobStatus.InProgress,
            "BR-SCHED-007: rescheduling never touches work that is printing");
    }
}
