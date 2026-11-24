using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.Commands.UrgentReprint;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;
using System.Text.Json;

namespace PrintGrid.OrderWorkflowTests;

public sealed class ReprintWorkflowRegressionTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 3, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(CostBearer.Lab)]
    [InlineData(CostBearer.System)]
    [InlineData(CostBearer.Customer)]
    public async Task Split_reprint_preserves_attribution_and_waits_for_both_labs(CostBearer bearer)
    {
        var original = NewJob(); original.Fail("Print failed");
        var faultLabId = bearer == CostBearer.Lab ? Guid.NewGuid() : (Guid?)null;
        var reprint = Job.CreateUrgentReprint(original, 1, faultLabId, bearer);
        var children = reprint.Split([3, 3]).Value;
        children.Sum(c => c.Quantity).Should().Be(6);
        children.Should().OnlyContain(c => c.ReprintIndex == 1 && c.OriginalJobId == original.Id &&
            c.ParentJobId == reprint.Id && c.ReprintOfJobId == null && c.Priority == JobPriority.Urgent &&
            c.CostBearer == bearer && c.FaultLabId == faultLabId && c.InternalDueDate == original.InternalDueDate &&
            c.DeliveryBufferDays == original.DeliveryBufferDays && c.LastMileDays == original.LastMileDays);
        var siblings = new[] { original, reprint }.Concat(children).ToList();
        PassInspection(children[0]);
        (await CompletedEvents(children[0], siblings)).Should().Be(0);
        PassInspection(children[1]);
        (await CompletedEvents(children[1], siblings)).Should().Be(1);
    }

    [Fact]
    public async Task A_second_reprint_of_one_split_branch_does_not_cover_a_failed_sibling()
    {
        var original = NewJob(); original.Fail("First failure");
        var reprint = Job.CreateUrgentReprint(original, 1);
        var children = reprint.Split([3, 3]).Value;
        children[0].Fail("Branch A failed"); children[1].Fail("Branch B failed");
        var replacementA = Job.CreateUrgentReprint(children[0], 2);
        PassInspection(replacementA);
        var siblings = new[] { original, reprint, children[0], children[1], replacementA };
        JobReprintLinks.HasReplacement(children[1], siblings).Should().BeFalse();
        (await CompletedEvents(replacementA, siblings)).Should().Be(0);
        var replacementB = Job.CreateUrgentReprint(children[1], 2);
        var grandChildren = replacementB.Split([1, 2]).Value;
        PassInspection(grandChildren[0]); PassInspection(grandChildren[1]);
        grandChildren.Sum(c => c.Quantity).Should().Be(3);
        (await CompletedEvents(grandChildren[1], [.. siblings, replacementB, .. grandChildren])).Should().Be(1);
    }

    [Fact]
    public async Task Legacy_reprint_split_with_missing_child_metadata_can_still_complete()
    {
        var original = NewJob(); original.Fail("Original failure");
        var reprint = Job.CreateUrgentReprint(original, 1);
        typeof(Job).GetProperty(nameof(Job.ReprintOfJobId))!.SetValue(reprint, null);
        var children = reprint.Split([3, 3]).Value;
        foreach (var child in children)
        {
            typeof(Job).GetProperty(nameof(Job.OriginalJobId))!.SetValue(child, null);
            typeof(Job).GetProperty(nameof(Job.ReprintIndex))!.SetValue(child, 0);
            PassInspection(child);
        }
        (await CompletedEvents(children[1], [original, reprint, .. children])).Should().Be(1);
    }

    [Fact]
    public async Task Repeated_request_creates_only_one_reprint_and_runs_assignment_once()
    {
        await using var db = Database();
        var original = NewJob(); original.Fail("Print failed");
        db.Add(original); await db.SaveChangesAsync();
        var engine = Engine();
        var handler = Handler(new JobRepository(db), new UnitOfWork(db), engine);
        var command = new CreateUrgentReprintCommand(original.Id, "Retry failed print");
        var first = await handler.Handle(command, default);
        var replay = await handler.Handle(command, default);
        first.IsSuccess.Should().BeTrue(); replay.IsSuccess.Should().BeTrue();
        replay.Value.ReprintJobId.Should().Be(first.Value.ReprintJobId);
        var replacements = await db.Set<Job>().Where(j => j.ReprintOfJobId == original.Id).ToListAsync();
        replacements.Should().ContainSingle().Which.Quantity.Should().Be(6);
        await engine.Received(1).AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_split_reprint_still_blocks_repeating_the_original_request()
    {
        await using var db = Database();
        var original = NewJob(); original.Fail("Print failed");
        var reprint = Job.CreateUrgentReprint(original, 1);
        var children = reprint.Split([3, 3]).Value;
        db.AddRange(original, reprint); db.AddRange(children); await db.SaveChangesAsync();
        var engine = Engine();
        var replay = await Handler(new JobRepository(db), new UnitOfWork(db), engine)
            .Handle(new CreateUrgentReprintCommand(original.Id, "Repeat"), default);
        replay.IsSuccess.Should().BeTrue(); replay.Value.ReprintJobId.Should().Be(reprint.Id);
        (await db.Set<Job>().CountAsync()).Should().Be(4);
        await engine.DidNotReceiveWithAnyArgs().AssignAsync(null!, null!);
    }

    [Fact]
    public async Task A_legacy_replacement_also_blocks_repeating_the_original_request()
    {
        await using var db = Database();
        var original = NewJob(); original.Fail("Print failed");
        var reprint = Job.CreateUrgentReprint(original, 1);
        typeof(Job).GetProperty(nameof(Job.ReprintOfJobId))!.SetValue(reprint, null);
        db.AddRange(original, reprint); await db.SaveChangesAsync();
        var replay = await Handler(new JobRepository(db), new UnitOfWork(db), Engine())
            .Handle(new CreateUrgentReprintCommand(original.Id, "Repeat"), default);
        replay.IsSuccess.Should().BeTrue(); replay.Value.ReprintJobId.Should().Be(reprint.Id);
        (await db.Set<Job>().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task A_failed_reprint_may_have_its_own_next_attempt_but_keeps_the_cap_after_split()
    {
        await using var db = Database();
        var original = NewJob(); original.Fail("Print failed");
        var first = Job.CreateUrgentReprint(original, 1);
        var children = first.Split([3, 3]).Value;
        children[0].Fail("First reprint failed");
        db.AddRange(original, first); db.AddRange(children); await db.SaveChangesAsync();
        var handler = Handler(new JobRepository(db), new UnitOfWork(db), Engine());
        var result = await handler.Handle(new CreateUrgentReprintCommand(children[0].Id, "Second attempt"), default);
        result.IsSuccess.Should().BeTrue(); result.Value.EscalatedToOps.Should().BeFalse();
        var second = await db.Set<Job>().SingleAsync(j => j.Id == result.Value.ReprintJobId);
        second.ReprintIndex.Should().Be(2); second.Quantity.Should().Be(3);
        second.ReprintOfJobId.Should().Be(children[0].Id);
        second.Fail("Second attempt failed"); await db.SaveChangesAsync();
        var limit = await handler.Handle(new CreateUrgentReprintCommand(second.Id, "Third attempt"), default);
        limit.IsSuccess.Should().BeTrue(); limit.Value.EscalatedToOps.Should().BeTrue();
        (await db.Set<Job>().CountAsync(j => j.ReprintOfJobId == second.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Losing_a_reprint_insert_race_never_assigns_or_renegotiates_a_phantom_job()
    {
        var original = NewJob(); original.Fail("Print failed");
        var repository = Substitute.For<IJobRepository>();
        repository.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);
        var unit = Substitute.For<IUnitOfWork>();
        unit.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>()).Returns(false);
        var engine = Engine(); var shortfall = Substitute.For<IPlacementShortfallService>();
        var handler = new CreateUrgentReprintCommandHandler(repository, engine, Substitute.For<IOpsEscalationRepository>(),
            shortfall, unit, Clock(), NullLogger<CreateUrgentReprintCommandHandler>.Instance);
        (await handler.Handle(new CreateUrgentReprintCommand(original.Id, "Concurrent request"), default)).IsFailure.Should().BeTrue();
        await engine.DidNotReceiveWithAnyArgs().AssignAsync(null!, null!);
        await shortfall.DidNotReceiveWithAnyArgs().HandleAsync(null!, null!);
    }

    [Fact]
    public async Task Concurrent_loser_returns_the_committed_replacement_without_assigning_it_again()
    {
        var original = NewJob(); original.Fail("Print failed");
        var winner = Job.CreateUrgentReprint(original, 1, costBearer: CostBearer.System);
        var repository = Substitute.For<IJobRepository>();
        repository.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);
        repository.GetByOrderItemIdAsync(original.OrderItemId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Job>>([original]), Task.FromResult<IReadOnlyList<Job>>([original, winner]));
        var unit = Substitute.For<IUnitOfWork>();
        unit.TrySaveChangesSafeAsync(Arg.Any<CancellationToken>()).Returns(false);
        var engine = Engine();
        var replay = await Handler(repository, unit, engine).Handle(new CreateUrgentReprintCommand(original.Id, "Concurrent request"), default);
        replay.IsSuccess.Should().BeTrue(); replay.Value.ReprintJobId.Should().Be(winner.Id);
        winner.CostBearer.Should().Be(CostBearer.System);
        await engine.DidNotReceiveWithAnyArgs().AssignAsync(null!, null!);
    }

    [Fact]
    public void Relational_model_allows_only_one_direct_replacement_per_failed_job()
    {
        using var db = Database();
        var index = db.Model.FindEntityType(typeof(Job))!.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Job.ReprintOfJobId));
        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"ReprintOfJobId\" IS NOT NULL");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Statistics_keep_history_but_clear_recovered_split_reprint_incidents(bool legacy)
    {
        await using var db = Database();
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), 6, PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m), Money.Of(1000), 30, 10, 500, 500, 20, 20, 20);
        quote.MarkReady(DateOnly.FromDateTime(Now).AddDays(10), "test");
        var order = Order.CreateFromQuote(quote, Address.Create("1 A", "B", "C", "HCM", ""), "PG-REPRINT", 50);
        var original = NewJob(order.Items.Single().Id); original.Fail("Print failed");
        var reprint = Job.CreateUrgentReprint(original, 1);
        if (legacy) typeof(Job).GetProperty(nameof(Job.ReprintOfJobId))!.SetValue(reprint, null);
        var children = reprint.Split([3, 3]).Value;
        foreach (var child in children) PassInspection(child);
        db.AddRange(order, original, reprint); db.AddRange(children); await db.SaveChangesAsync();
        var summary = await Report(db);
        summary.GetProperty("totalOrders").GetInt32().Should().Be(1);
        summary.GetProperty("incidentOrders").GetInt32().Should().Be(1);
        summary.GetProperty("openIncidentOrders").GetInt32().Should().Be(0);
        // One branch fails: the other branch's later attempt must not hide this incident.
        // Model later failed attempts in the stored history, then replace only branch B.
        db.Entry(children[0]).Property(j => j.Status).CurrentValue = JobStatus.Failed;
        db.Entry(children[1]).Property(j => j.Status).CurrentValue = JobStatus.Failed;
        var replacementB = Job.CreateUrgentReprint(children[1], 2);
        PassInspection(replacementB); db.Add(replacementB);
        await db.SaveChangesAsync();
        (await Report(db)).GetProperty("openIncidentOrders").GetInt32().Should().Be(1);
        var replacementA = Job.CreateUrgentReprint(children[0], 2);
        PassInspection(replacementA); db.Add(replacementA); await db.SaveChangesAsync();
        (await Report(db)).GetProperty("openIncidentOrders").GetInt32().Should().Be(0);
        var detail = (OkObjectResult)await new OpsOrdersController(db).Detail(order.Id, default);
        var json = JsonSerializer.SerializeToElement(detail.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.GetProperty("items")[0].GetProperty("completedQuantity").GetInt32().Should().Be(6);
    }

    private static async Task<JsonElement> Report(PrintGridDbContext db)
    {
        var response = (OkObjectResult)await new OpsOrdersController(db).List(null, null, "all", null);
        return JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web)).GetProperty("summary");
    }

    private static Job NewJob(Guid? itemId = null) => Job.Create(itemId ?? Guid.NewGuid(), Guid.NewGuid(),
        JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .3m, PrintTechnology.Fdm, 60),
        180, DateOnly.FromDateTime(Now).AddDays(10), 6, deliveryBufferDays: 5, lastMileDays: 2);

    private static void PassInspection(Job job)
    {
        job.AssignTo(Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddHours(3), 1m).IsSuccess.Should().BeTrue();
        job.Accept(Now).IsSuccess.Should().BeTrue(); job.Start(Now).IsSuccess.Should().BeTrue();
        job.Complete(Now.AddHours(3), 180, "QC", ["proof.jpg"]).IsSuccess.Should().BeTrue();
        job.ReviewQcProof(true, Guid.NewGuid(), null, Now.AddHours(3)).IsSuccess.Should().BeTrue();
        job.MarkInspectionPassed(["hub.jpg"]).IsSuccess.Should().BeTrue();
    }

    private static async Task<int> CompletedEvents(Job passed, IReadOnlyList<Job> siblings)
    {
        var jobs = Substitute.For<IJobRepository>(); var publisher = Substitute.For<IPublisher>();
        jobs.GetByIdAsync(passed.Id, Arg.Any<CancellationToken>()).Returns(passed);
        jobs.GetByOrderItemIdAsync(passed.OrderItemId, Arg.Any<CancellationToken>()).Returns(siblings);
        await new JobStatusIntegrationPublisher(publisher, jobs, NullLogger<JobStatusIntegrationPublisher>.Instance)
            .Handle(new JobInspectionPassedEvent(passed.Id), default);
        return publisher.ReceivedCalls().Count(call => call.GetArguments().Any(arg => arg is ProductionStatusChangedIntegrationEvent { NewStatus: "Completed" }));
    }

    private static CreateUrgentReprintCommandHandler Handler(IJobRepository repository, IUnitOfWork unit, IAssignmentEngine engine) =>
        new(repository, engine, Substitute.For<IOpsEscalationRepository>(), Substitute.For<IPlacementShortfallService>(),
            unit, Clock(), NullLogger<CreateUrgentReprintCommandHandler>.Instance);
    private static IAssignmentEngine Engine()
    {
        var engine = Substitute.For<IAssignmentEngine>();
        engine.AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AssignmentOutcome(false, null, AssignmentOutcomes.PlacementRace, null, [], [], 0, false, null));
        return engine;
    }
    private static IDateTimeProvider Clock()
    {
        var clock = Substitute.For<IDateTimeProvider>(); clock.UtcNow.Returns(Now); return clock;
    }
    private static PrintGridDbContext Database()
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        return new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
}
