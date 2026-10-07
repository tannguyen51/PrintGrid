using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Commands.AssignJob;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Scheduling;

/// <summary>
/// FR-HUB-003 / BR-RESCHED-003/004 ("in lại khẩn"): the reprint keeps the original deadline,
/// is charged to the at-fault lab, and stops after the cap so a human takes over.
/// </summary>
public class UrgentReprintTests
{
    private readonly IJobRepository _jobs = Substitute.For<IJobRepository>();
    private readonly IAssignmentEngine _engine = Substitute.For<IAssignmentEngine>();
    private readonly IOpsEscalationRepository _escalations = Substitute.For<IOpsEscalationRepository>();
    private readonly IPlacementShortfallService _shortfall = Substitute.For<IPlacementShortfallService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTime _now = new(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);

    public UrgentReprintTests() => _clock.UtcNow.Returns(_now);

    private CreateUrgentReprintCommandHandler Handler() => new(
        _jobs, _engine, _escalations, _shortfall, _unitOfWork, _clock,
        NullLogger<CreateUrgentReprintCommandHandler>.Instance);

    /// <summary>A job that printed, was inspected and failed — the state a reprint starts from.</summary>
    private static Job FailedJob(Guid? labId = null)
    {
        var job = Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            estimatedPrintMinutes: 60,
            internalDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));

        var start = DateTime.UtcNow.AddHours(1);
        job.AssignTo(labId ?? Guid.NewGuid(), Guid.NewGuid(), start, start.AddHours(1), 0.9m);
        job.Accept(DateTime.UtcNow);
        job.Start(DateTime.UtcNow);
        job.Complete(DateTime.UtcNow, 55, "Layer adhesion OK", new[] { "lab-qc/photo-1.jpg" });
        job.Fail("layer shift");
        return job;
    }

    private static AssignmentOutcome Assigned(Guid jobId)
    {
        var start = DateTime.UtcNow.AddHours(2);
        return new AssignmentOutcome(
            true,
            new AssignmentResultDto(
                jobId, Guid.NewGuid(), Guid.NewGuid(), start, start.AddHours(1), 0.87m,
                new Dictionary<string, decimal> { ["due_date_slack"] = 0.9m }),
            AssignmentOutcomes.Assigned, null, [], [], 80, false, null);
    }

    private static AssignmentOutcome NotAssigned(string outcome, string reason) =>
        new(false, null, outcome, reason, [], [], 80, false, null);

    [Fact]
    public async Task A_reprint_keeps_the_original_deadline_and_is_charged_to_the_lab()
    {
        var printedBy = Guid.NewGuid();
        var original = FailedJob(printedBy);
        _jobs.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

        Job? created = null;
        await _jobs.AddAsync(Arg.Do<Job>(j => created = j), Arg.Any<CancellationToken>());

        Job? placed = null;
        AssignmentRequest? capturedRequest = null;
        _engine.AssignAsync(
                Arg.Do<Job>(j => placed = j),
                Arg.Do<AssignmentRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>())
            .Returns(ci => Assigned(ci.Arg<Job>().Id));

        var result = await Handler().Handle(
            new CreateUrgentReprintCommand(original.Id, "layer shift"), default);

        result.IsSuccess.Should().BeTrue();
        created.Should().NotBeNull();

        created!.Priority.Should().Be(JobPriority.Urgent, "BR-RESCHED-003 creates the reprint as URGENT");
        created.InternalDueDate.Should().Be(original.InternalDueDate,
            "the reprint inherits the original deadline so the promised date is preserved");
        created.OrderItemId.Should().Be(original.OrderItemId);
        created.ReprintIndex.Should().Be(1);
        created.OriginalJobId.Should().Be(original.Id);
        created.CostBearer.Should().Be(CostBearer.Lab,
            "13-AC: the cost of a fault reprint is charged to the lab");
        created.FaultLabId.Should().Be(printedBy);
        created.Status.Should().Be(JobStatus.Pending);

        placed.Should().BeSameAs(created, "the reprint goes straight through the assign engine");
        capturedRequest!.Trigger.Should().Be(AssignmentTriggers.UrgentReprint);
        capturedRequest.ExcludeLabId.Should().Be(printedBy,
            "the lab at fault must not be handed the reprint (same rule as a refusal)");
        created.DomainEvents.Should().ContainSingle(e => e is JobReprintCreatedEvent);
        result.Value!.ReprintJobId.Should().Be(created.Id);
        result.Value.EscalatedToOps.Should().BeFalse();
    }

    [Fact]
    public async Task The_second_reprint_is_still_allowed()
    {
        var original = FailedJob();
        var firstReprint = Job.CreateUrgentReprint(original, 1);
        firstReprint.Fail("warped");
        _jobs.GetByIdAsync(firstReprint.Id, Arg.Any<CancellationToken>()).Returns(firstReprint);
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => Assigned(ci.Arg<Job>().Id));

        Job? created = null;
        await _jobs.AddAsync(Arg.Do<Job>(j => created = j), Arg.Any<CancellationToken>());

        var result = await Handler().Handle(new CreateUrgentReprintCommand(firstReprint.Id, "warped"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalatedToOps.Should().BeFalse();
        created!.ReprintIndex.Should().Be(2, "the cap allows exactly two automatic reprints (B14)");
        created.OriginalJobId.Should().Be(original.Id, "the chain always points back to the original job");
        await _escalations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Past_the_cap_the_case_goes_to_operations_instead_of_being_reprinted()
    {
        var original = FailedJob();
        var lastAllowed = Job.CreateUrgentReprint(original, ReprintPolicy.MaxAutoReprints);
        lastAllowed.Fail("third failure");

        _jobs.GetByIdAsync(lastAllowed.Id, Arg.Any<CancellationToken>()).Returns(lastAllowed);

        OpsEscalation? escalation = null;
        await _escalations.AddAsync(Arg.Do<OpsEscalation>(e => escalation = e), Arg.Any<CancellationToken>());

        var result = await Handler().Handle(new CreateUrgentReprintCommand(lastAllowed.Id, "third failure"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalatedToOps.Should().BeTrue();
        result.Value.ReprintJobId.Should().BeNull();

        escalation.Should().NotBeNull();
        escalation!.Kind.Should().Be(OpsEscalationKind.ReprintLimitExceeded);
        escalation.Status.Should().Be(OpsEscalationStatus.Open);

        await _jobs.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _engine.DidNotReceiveWithAnyArgs().AssignAsync(default!, default!, default);
    }

    [Fact]
    public async Task A_reprint_that_cannot_hold_the_inherited_deadline_is_renegotiated_with_the_customer()
    {
        var original = FailedJob();
        _jobs.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

        Job? created = null;
        await _jobs.AddAsync(Arg.Do<Job>(j => created = j), Arg.Any<CancellationToken>());
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotAssigned(AssignmentOutcomes.NoFeasibleSlot, "No capable machine has capacity"));

        await Handler().Handle(new CreateUrgentReprintCommand(original.Id, "layer shift"), default);

        await _shortfall.Received(1).HandleAsync(
            Arg.Is<Job>(j => j.Id == created!.Id),
            "No capable machine has capacity",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_lost_placement_race_leaves_the_reprint_pending_for_the_ops_board()
    {
        var original = FailedJob();
        _jobs.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);
        _engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotAssigned(AssignmentOutcomes.PlacementRace, "boom"));

        var result = await Handler().Handle(new CreateUrgentReprintCommand(original.Id, "layer shift"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlannedStartUtc.Should().BeNull();
        await _shortfall.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task Only_a_failed_job_can_be_reprinted()
    {
        var pending = Job.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SpecBuilder.Default(),
            60,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        _jobs.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var result = await Handler().Handle(new CreateUrgentReprintCommand(pending.Id, "no reason"), default);

        result.IsFailure.Should().BeTrue();
        await _jobs.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
