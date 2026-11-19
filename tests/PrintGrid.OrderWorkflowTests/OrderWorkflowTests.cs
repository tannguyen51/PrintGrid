using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Api.Controllers;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PrintGrid.OrderWorkflowTests;

public sealed class OrderWorkflowTests
{
    private static readonly DateTime Now = DateTime.SpecifyKind(new DateTime(2026, 11, 19, 3, 0, 0), DateTimeKind.Utc);

    [Theory]
    [InlineData(4, "Automatic", true)]
    [InlineData(5, "Manual", false)]
    [InlineData(100, "Manual", false)]
    public void Quantity_routing_never_creates_a_payment(int quantity, string mode, bool auto)
    {
        var order = Order.CreateFromQuote(ReadyQuote(quantity), Address.Create("1 A", "B", "C", "HCM", ""), "PG-TEST", 50);
        order.DispatchMode.Should().Be(mode);
        order.Status.Should().Be(OrderStatus.PaymentPending);
        order.DomainEvents.Should().NotContain(e => e is OrderConfirmedEvent);
        order.ApplyPayment(order.DepositAmount).IsSuccess.Should().BeTrue();
        order.DomainEvents.OfType<OrderConfirmedEvent>().Single().AutoAssign.Should().Be(auto);
    }

    [Fact]
    public void Quantity_threshold_counts_all_items()
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        AddItem(quote, 3); AddItem(quote, 2);
        quote.MarkReady(DateOnly.FromDateTime(Now).AddDays(10), "test");
        var order = Order.CreateFromQuote(quote, Address.Create("1 A", "B", "C", "HCM", ""), "PG-MULTI", 50);
        order.DispatchMode.Should().Be("Manual");
    }

    [Theory]
    [InlineData("", "B", "C", "HCM", true)]
    [InlineData("A", "", "C", "HCM", true)]
    [InlineData("A", "B", "", "HCM", true)]
    [InlineData("A", "B", "C", "", true)]
    [InlineData("A", "B", "C", "HCM", false)]
    public void Address_and_requested_date_are_required(string street, string ward, string district, string city, bool hasDate)
    {
        var command = new PlaceOrderCommand(Guid.NewGuid(), Guid.NewGuid(), street, ward, district, city, "", true,
            RequestedDeliveryDate: hasDate ? DateOnly.FromDateTime(Now).AddDays(10) : null);
        new PlaceOrderCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Delivery_rechecks_capacity_and_adds_last_mile_without_overwriting_the_quote()
    {
        var quote = ReadyQuote(2);
        var capacity = Substitute.For<IProductionCapacityProbe>();
        capacity.FindEarliestFeasibleDeliveryAsync(Arg.Any<IReadOnlyList<TrialPlacementLine>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new TrialPlacementResult(quote.PromisedDeliveryDate.AddDays(1), "live"));
        var result = await new OrderDeliveryService(capacity, Clock(), Config()).AssessAsync(quote, quote.PromisedDeliveryDate, default);
        result.IsSuccess.Should().BeTrue();
        result.Value.CanMeetRequestedDate.Should().BeFalse();
        result.Value.EarliestDeliveryDate.Should().Be(quote.PromisedDeliveryDate.AddDays(3));
        quote.Status.Should().Be(QuoteStatus.Ready);
    }

    [Theory]
    [InlineData(4, true, 1)]
    [InlineData(5, false, 0)]
    [InlineData(5, true, 0)]
    public async Task Only_paid_small_order_events_enter_automatic_assignment(int quantity, bool auto, int calls)
    {
        var jobs = Substitute.For<IJobRepository>();
        var engine = Substitute.For<IAssignmentEngine>();
        var notification = new OrderConfirmedEvent(Guid.NewGuid(), [new OrderConfirmedItem(Guid.NewGuid(), Guid.NewGuid(), quantity,
            "PLA", "BLACK", .2m, 30, 10, 20, 20, 20)], DateOnly.FromDateTime(Now).AddDays(10), auto);
        await new OrderConfirmedEventHandler(jobs, Substitute.For<IUnitOfWork>(), NullLogger<OrderConfirmedEventHandler>.Instance, engine)
            .Handle(notification, default);
        await engine.Received(calls).AssignAsync(Arg.Any<Job>(), Arg.Any<AssignmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replayed_confirmation_does_not_duplicate_jobs()
    {
        var jobs = Substitute.For<IJobRepository>();
        var itemId = Guid.NewGuid();
        jobs.GetByOrderItemIdAsync(itemId, Arg.Any<CancellationToken>()).Returns([NewJob(itemId, 2)]);
        var notification = new OrderConfirmedEvent(Guid.NewGuid(), [new OrderConfirmedItem(itemId, Guid.NewGuid(), 2, "PLA", "BLACK", .2m, 30, 10, 20, 20, 20)], DateOnly.FromDateTime(Now).AddDays(10));
        await new OrderConfirmedEventHandler(jobs, Substitute.For<IUnitOfWork>(), NullLogger<OrderConfirmedEventHandler>.Instance).Handle(notification, default);
        await jobs.DidNotReceive().AddAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Preview_is_read_only_and_confirmation_splits_six_units_across_two_labs_once()
    {
        await using var db = Database();
        var (job, _, _) = await SeedSplit(db);
        var service = new SplitDispatchService(db, new MachineTimelineService(db), Clock());
        var proposal = await service.PreviewAsync(job.Id, default);
        proposal.IsSuccess.Should().BeTrue(proposal.Error.Message);
        proposal.Value.Batches.Should().HaveCount(2);
        proposal.Value.Batches.Sum(b => b.Quantity).Should().Be(6);
        (await db.Set<Job>().CountAsync()).Should().Be(1);
        job.Status.Should().Be(JobStatus.Pending);
        var confirmed = await service.ConfirmAsync(proposal.Value, default);
        confirmed.IsSuccess.Should().BeTrue(confirmed.Error.Message);
        db.ChangeTracker.Clear();
        var children = await db.Set<Job>().Where(j => j.ParentJobId == job.Id).ToListAsync();
        children.Should().HaveCount(2);
        children.Sum(j => j.Quantity).Should().Be(6);
        children.Select(j => j.LabId).Distinct().Should().HaveCount(2);
        children.Should().OnlyContain(j => j.Status == JobStatus.Assigned);
        (await service.ConfirmAsync(proposal.Value, default)).IsFailure.Should().BeTrue();
        (await db.Set<Job>().CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Changed_machine_schedule_rejects_proposal_without_splitting_original()
    {
        await using var db = Database();
        var (job, _, _) = await SeedSplit(db);
        var service = new SplitDispatchService(db, new MachineTimelineService(db), Clock());
        var proposal = (await service.PreviewAsync(job.Id, default)).Value;
        var batch = proposal.Batches[0];
        var blocker = NewJob(Guid.NewGuid(), 1);
        blocker.AssignTo(batch.LabId, batch.MachineId, batch.PlannedStartUtc, batch.PlannedEndUtc, 0, Now);
        db.Add(blocker); await db.SaveChangesAsync();
        (await service.ConfirmAsync(proposal, default)).IsFailure.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Pending);
        (await db.Set<Job>().AnyAsync(j => j.ParentJobId == job.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Expired_or_wrong_quantity_proposals_never_mutate_jobs()
    {
        await using var db = Database();
        var (job, _, _) = await SeedSplit(db);
        var service = new SplitDispatchService(db, new MachineTimelineService(db), Clock());
        var proposal = (await service.PreviewAsync(job.Id, default)).Value;
        (await service.ConfirmAsync(proposal with { ExpiresAtUtc = Now.AddSeconds(-1) }, default)).IsFailure.Should().BeTrue();
        var bad = proposal with { Batches = [proposal.Batches[0] with { Quantity = 100 }, proposal.Batches[1]] };
        (await service.ConfirmAsync(bad, default)).IsFailure.Should().BeTrue();
        (await db.Set<Job>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Insufficient_deposit_blocks_split_even_when_the_job_exists()
    {
        await using var db = Database();
        var (job, _, _) = await SeedSplit(db);
        var order = await db.Set<Order>().SingleAsync();
        db.Entry(order).Property(o => o.PaidAmount).CurrentValue = 0;
        await db.SaveChangesAsync();
        var service = new SplitDispatchService(db, new MachineTimelineService(db), Clock());
        (await service.PreviewAsync(job.Id, default)).IsFailure.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Pending);
    }

    [Fact]
    public async Task Customer_progress_is_owner_scoped_and_never_exposes_lab_identity()
    {
        await using var db = Database();
        var (job, _, _) = await SeedSplit(db);
        var order = await db.Set<Order>().SingleAsync();
        var service = new SplitDispatchService(db, new MachineTimelineService(db), Clock());
        await service.ConfirmAsync((await service.PreviewAsync(job.Id, default)).Value, default);
        var controller = new OrderProductionController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        (await controller.Progress(order.Id, default)).Should().BeOfType<NotFoundResult>();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, order.CustomerId.ToString())], "test"));
        var response = (OkObjectResult)await controller.Progress(order.Id, default);
        var json = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json[0].GetProperty("batches").GetArrayLength().Should().Be(2);
        json[0].GetProperty("completedQuantity").GetInt32().Should().Be(0);
        json.ToString().Should().NotContain("labId").And.NotContain("labName").And.NotContain("machineId");
    }

    [Fact]
    public async Task Statistics_date_range_includes_the_whole_Vietnam_day_and_paginates_orders()
    {
        await using var db = Database();
        foreach (var (number, created) in new[] { ("PG-OLD", Now.Date.AddDays(-1).AddHours(16)),
            ("PG-START", Now.Date.AddDays(-1).AddHours(17)), ("PG-END", Now.Date.AddHours(16).AddMinutes(59)), ("PG-NEXT", Now.Date.AddHours(17)) })
        {
            var order = Order.CreateFromQuote(ReadyQuote(1), Address.Create("1 A", "B", "C", "HCM", ""), number);
            db.Add(order); db.Entry(order).Property(o => o.CreatedAt).CurrentValue = created;
        }
        await db.SaveChangesAsync();
        var controller = new OpsOrdersController(db);
        var response = (OkObjectResult)await controller.List(DateOnly.FromDateTime(Now), DateOnly.FromDateTime(Now), "all", null, 1, 1);
        var json = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.GetProperty("summary").GetProperty("totalOrders").GetInt32().Should().Be(2);
        json.GetProperty("totalCount").GetInt32().Should().Be(2);
        json.GetProperty("items").GetArrayLength().Should().Be(1);
        json.GetProperty("items")[0].GetProperty("orderNumber").GetString().Should().Be("PG-END");
    }

    [Fact]
    public void Split_and_reprints_keep_the_frozen_delivery_buffer()
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(),
            JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .3m, PrintTechnology.Fdm, 60),
            180, DateOnly.FromDateTime(Now).AddDays(7), 6, deliveryBufferDays: 7, lastMileDays: 3);
        var batches = job.Split([3, 3]).Value;
        batches.Should().OnlyContain(j => j.DeliveryBufferDays == 7 && j.LastMileDays == 3);
        var reprint = Job.CreateUrgentReprint(batches[0], 1);
        reprint.DeliveryBufferDays.Should().Be(7);
        reprint.LastMileDays.Should().Be(3);
    }

    [Fact]
    public async Task Statistics_count_orders_once_and_distinguish_refund_and_recovered_failure()
    {
        await using var db = Database();
        var first = Order.CreateFromQuote(ReadyQuote(6), Address.Create("1 A", "B", "C", "HCM", ""), "PG-1", 50);
        first.ApplyPayment(first.TotalPrice.Amount);
        first.RefundWithinOriginalOrderScope(100);
        var second = Order.CreateFromQuote(ReadyQuote(1), Address.Create("1 A", "B", "C", "HCM", ""), "PG-2", 50);
        second.ApplyPayment(second.DepositAmount);
        second.RefundWithinOriginalOrderScope(second.PaidAmount);
        db.AddRange(first, second);
        var original = NewJob(first.Items.Single().Id, 6);
        var children = original.Split([3, 3]).Value;
        db.Add(original); db.AddRange(children);
        var failed = NewJob(first.Items.Single().Id, 1);
        db.Add(failed); db.Entry(failed).Property(j => j.Status).CurrentValue = JobStatus.Failed;
        db.Entry(first).Property(o => o.Status).CurrentValue = OrderStatus.Delivered;
        await db.SaveChangesAsync();
        var response = (OkObjectResult)await new OpsOrdersController(db).List(null, null, "all", null, ct: default);
        var json = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.GetProperty("summary").GetProperty("totalOrders").GetInt32().Should().Be(2);
        json.GetProperty("summary").GetProperty("incidentOrders").GetInt32().Should().Be(1);
        json.GetProperty("summary").GetProperty("deliveredOrders").GetInt32().Should().Be(1);
        json.GetProperty("summary").GetProperty("openIncidentOrders").GetInt32().Should().Be(0);
        json.GetProperty("summary").GetProperty("partialRefundOrders").GetInt32().Should().Be(1);
        json.GetProperty("summary").GetProperty("fullyRefundedOrders").GetInt32().Should().Be(1);
        json.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Item_is_not_completed_until_all_split_batches_pass_inspection()
    {
        var root = NewJob(Guid.NewGuid(), 6);
        var children = root.Split([3, 3]).Value;
        await using var db = Database();
        db.Add(root); db.AddRange(children); await db.SaveChangesAsync();
        var repository = Substitute.For<IJobRepository>();
        var publisher = Substitute.For<IPublisher>();
        repository.GetByIdAsync(children[0].Id, Arg.Any<CancellationToken>()).Returns(children[0]);
        repository.GetByOrderItemIdAsync(root.OrderItemId, Arg.Any<CancellationToken>()).Returns([root, .. children]);
        var handler = new JobStatusIntegrationPublisher(publisher, repository, NullLogger<JobStatusIntegrationPublisher>.Instance);
        db.Entry(children[0]).Property(j => j.Status).CurrentValue = JobStatus.Completed;
        await handler.Handle(new JobInspectionPassedEvent(children[0].Id), default);
        await publisher.DidNotReceive().Publish(Arg.Is<ProductionStatusChangedIntegrationEvent>(e => e.NewStatus == "Completed"), Arg.Any<CancellationToken>());
        db.Entry(children[1]).Property(j => j.Status).CurrentValue = JobStatus.Completed;
        await handler.Handle(new JobInspectionPassedEvent(children[0].Id), default);
        await publisher.Received(1).Publish(Arg.Is<ProductionStatusChangedIntegrationEvent>(e => e.NewStatus == "Completed"), Arg.Any<CancellationToken>());
    }

    private static async Task<(Job Job, Lab First, Lab Second)> SeedSplit(PrintGridDbContext db)
    {
        var profile = CuraProfile.Import("PLA", "ACME", "MODEL", "PLA", "1", "5.11", .4m, 1.75m, 1, 1, 220, 220, 250, true, 200, 60, "{}");
        var first = Lab.Onboard("Lab A", "HCM", 1);
        var second = Lab.Onboard("Lab B", "HCM", 1);
        foreach (var lab in new[] { first, second })
        {
            var machine = Machine.Register(lab.Id, "Printer", "MODEL", PrintTechnology.Fdm, BuildVolume.Create(220, 220, 250), .1m, .1m, ["PLA"]);
            machine.SetVerifiedProfiles([profile.Id], true, true);
            lab.AddMachine(machine);
        }
        var order = Order.CreateFromQuote(ReadyQuote(6), Address.Create("1 A", "B", "C", "HCM", ""), "PG-SPLIT", 50);
        order.ApplyPayment(order.TotalPrice.Amount);
        var job = NewJob(order.Items.Single().Id, 6);
        job.SetQuotedProduction("{\"Quantity\":6,\"Colors\":[{\"ColorCode\":\"BLACK\",\"Grams\":60}]}", first.Machines.Single().Id);
        db.AddRange(profile, first, second, order, job); await db.SaveChangesAsync();
        return (job, first, second);
    }

    private static Job NewJob(Guid itemId, int quantity) => Job.Create(itemId, Guid.NewGuid(),
        JobSpecification.Create(BuildVolume.Create(20, 20, 20), "PLA", "BLACK", .2m, .3m, PrintTechnology.Fdm, 10m * quantity),
        30 * quantity, DateOnly.FromDateTime(Now).AddDays(7), quantity);
    private static void AddItem(Quote quote, int quantity) => quote.AddItem(Guid.NewGuid(), quantity,
        PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m), Money.Of(1000), 30, 10, 500, 500, 20, 20, 20);
    private static Quote ReadyQuote(int quantity)
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48)); AddItem(quote, quantity);
        quote.MarkReady(DateOnly.FromDateTime(Now).AddDays(7), "test"); return quote;
    }
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection().Build();
    private static IDateTimeProvider Clock()
    {
        var clock = Substitute.For<IDateTimeProvider>(); clock.UtcNow.Returns(Now); clock.Today.Returns(DateOnly.FromDateTime(Now)); return clock;
    }
    private static PrintGridDbContext Database()
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        return new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
}
