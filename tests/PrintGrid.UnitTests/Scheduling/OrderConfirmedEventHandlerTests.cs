using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// Auto-assign right after the order is confirmed: every newly created job goes through the
/// shared engine once, and one bad job cannot stop the rest of the order (FR-SCHED-006/007).
/// </summary>
public sealed class OrderConfirmedEventHandlerTests
{
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAssignmentEngine _engine = Substitute.For<IAssignmentEngine>();
    private readonly IPlacementShortfallService _shortfall = Substitute.For<IPlacementShortfallService>();

    private OrderConfirmedEventHandler Handler() =>
        new(_jobs, _unitOfWork, _engine, _shortfall, NullLogger<OrderConfirmedEventHandler>.Instance);

    private static readonly AssignmentOutcome Assigned = new(
        Assigned: true,
        Result: new PrintGrid.Modules.Scheduling.Application.Commands.AssignJob.AssignmentResultDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 0.9m,
            new Dictionary<string, decimal>()),
        Outcome: AssignmentOutcomes.Assigned,
        Reason: null,
        Ranking: [],
        Rejections: [],
        ElapsedMs: 120,
        BudgetExceeded: false,
        EarliestFeasibleEndUtc: null);

    private static AssignmentOutcome Unplaced(string outcome, string? reason = null) =>
        new(false, null, outcome, reason, [], [], ElapsedMs: 120, BudgetExceeded: false, EarliestFeasibleEndUtc: null);

    private static OrderConfirmedEvent Event(params OrderConfirmedItem[] items) =>
        new(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), items);

    private static OrderConfirmedItem Item(int minutes = 60, decimal grams = 45m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1, "PLA", "BLACK", 0.2m, 15, 0.3m,
            minutes, grams, 80m, 80m, 80m);

    private List<Job> CaptureCreatedJobs()
    {
        var created = new List<Job>();
        _jobs.AddAsync(Arg.Do<Job>(created.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return created;
    }

    [Fact]
    public async Task Each_new_job_is_auto_assigned_once_after_the_jobs_are_committed()
    {
        var created = CaptureCreatedJobs();
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Assigned);

        await Handler().Handle(Event(Item(), Item()), default);

        created.Should().HaveCount(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _engine.Received(2).AssignAsync(
            Arg.Any<Job>(),
            Arg.Is<AssignmentRequest>(request =>
                request.Trigger == AssignmentTriggers.InitialAssign &&
                request.TimeBudget == TimeSpan.FromSeconds(AssignmentEngine.DefaultTimeBudgetSeconds)),
            Arg.Any<CancellationToken>());

        // The engine must receive the SAME job instances that were persisted, not copies.
        foreach (var job in created)
            await _engine.Received(1).AssignAsync(job, Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>());

        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task A_job_with_no_feasible_slot_goes_to_the_date_change_flow()
    {
        CaptureCreatedJobs();
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Unplaced(AssignmentOutcomes.NoFeasibleSlot, "No capable machine has capacity"));

        await Handler().Handle(Event(Item()), default);

        await _shortfall.Received(1).HandleAsync(
            Arg.Any<Job>(), "No capable machine has capacity", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_job_with_no_capable_machine_goes_to_the_date_change_flow()
    {
        CaptureCreatedJobs();
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Unplaced(AssignmentOutcomes.NoCapableMachine, "No lab can satisfy the specification"));

        await Handler().Handle(Event(Item()), default);

        await _shortfall.Received(1).HandleAsync(
            Arg.Any<Job>(), "No lab can satisfy the specification", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_lost_placement_race_is_not_treated_as_a_shortfall()
    {
        CaptureCreatedJobs();
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Unplaced(AssignmentOutcomes.PlacementRace, "constraint lost"));

        await Handler().Handle(Event(Item()), default);

        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task A_failure_on_one_job_does_not_block_the_remaining_jobs()
    {
        var created = CaptureCreatedJobs();
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<AssignmentOutcome>>(_ => throw new InvalidOperationException("engine blew up"));

        var act = () => Handler().Handle(Event(Item(), Item(), Item()), default);

        await act.Should().NotThrowAsync();
        await _engine.Received(3).AssignAsync(
            Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>());
        created.Should().HaveCount(3);
    }
}
