using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// FR-SCHED-007: production events repair the unstarted plan, never touch work in progress,
/// and hand over to the date-change flow when the committed date cannot be held.
/// </summary>
public class ReschedulingTriggeredEventHandlerTests
{
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IAssignmentEngine _engine = Substitute.For<IAssignmentEngine>();
    private readonly IPlacementShortfallService _shortfall = Substitute.For<IPlacementShortfallService>();

    private ReschedulingTriggeredEventHandler Handler() =>
        new(_jobs, _engine, _shortfall, NullLogger<ReschedulingTriggeredEventHandler>.Instance);

    private static Job PendingJob() => Job.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        SpecBuilder.Default(),
        estimatedPrintMinutes: 60,
        internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

    private static Job PrintingJob()
    {
        var job = PendingJob();
        var start = DateTime.UtcNow.AddHours(1);
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), start, start.AddHours(1), 0.9m);
        job.Accept(DateTime.UtcNow);
        job.Start(DateTime.UtcNow);
        return job;
    }

    private static Job FailedJob()
    {
        var job = PendingJob();
        job.Fail("layer shift");
        return job;
    }

    private static AssignmentOutcome Outcome(bool assigned, string outcome, string? reason = null) =>
        new(assigned, null, outcome, reason, [], [], ElapsedMs: 120, BudgetExceeded: false, EarliestFeasibleEndUtc: null);

    private void EngineReturns(AssignmentOutcome outcome) =>
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

    [Fact]
    public async Task Reschedule_is_skipped_for_a_job_that_is_already_printing()
    {
        var job = PrintingJob();
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        await Handler().Handle(new ReschedulingTriggeredEvent(job.Id, "lab_decline", job.InternalDueDate), default);

        await _engine.DidNotReceiveWithAnyArgs()
            .AssignAsync(default!, default!, default);
        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
        job.Status.Should().Be(JobStatus.InProgress,
            "BR-SCHED-007 locks jobs with status IN_PROGRESS");
    }

    [Fact]
    public async Task A_failed_job_is_left_to_the_reprint_flow_instead_of_being_replanned()
    {
        var job = FailedJob();
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        await Handler().Handle(new ReschedulingTriggeredEvent(job.Id, "print_failure", job.InternalDueDate), default);

        await _engine.DidNotReceiveWithAnyArgs().AssignAsync(default!, default!, default);
        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task A_declined_job_is_placed_again_without_asking_the_customer()
    {
        var job = PendingJob();
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        EngineReturns(Outcome(assigned: true, AssignmentOutcomes.Assigned));
        var declinedLabId = Guid.NewGuid();

        await Handler().Handle(
            new ReschedulingTriggeredEvent(job.Id, "lab_decline", job.InternalDueDate, declinedLabId), default);

        await _engine.Received(1).AssignAsync(
            job,
            Arg.Is<AssignmentRequest>(r =>
                r.Trigger == "lab_decline" &&
                r.TimeBudget == TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds) &&
                r.ExcludeLabId == declinedLabId),
            Arg.Any<CancellationToken>());
        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task A_job_with_no_feasible_slot_goes_to_the_date_change_flow()
    {
        var job = PendingJob();
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        EngineReturns(Outcome(false, AssignmentOutcomes.NoFeasibleSlot, "No capable machine has capacity"));

        await Handler().Handle(new ReschedulingTriggeredEvent(job.Id, "lab_decline", job.InternalDueDate), default);

        await _shortfall.Received(1).HandleAsync(
            job, "No capable machine has capacity", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_lost_placement_race_is_retried_from_the_board_rather_than_renegotiated()
    {
        var job = PendingJob();
        _jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        EngineReturns(Outcome(false, AssignmentOutcomes.PlacementRace, "boom"));

        await Handler().Handle(new ReschedulingTriggeredEvent(job.Id, "lab_decline", job.InternalDueDate), default);

        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task An_unknown_job_is_ignored()
    {
        _jobs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Job?)null);

        await Handler().Handle(new ReschedulingTriggeredEvent(Guid.NewGuid(), "lab_decline", DateOnly.FromDateTime(DateTime.UtcNow)), default);

        await _engine.DidNotReceiveWithAnyArgs().AssignAsync(default!, default!, default);
    }
}
