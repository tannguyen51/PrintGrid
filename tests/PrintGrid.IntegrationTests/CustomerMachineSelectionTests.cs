using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Api.Bootstrap;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Customer.Infrastructure.Services;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
using PrintGrid.Modules.Scheduling.Infrastructure.Scheduling;
using PrintGrid.SharedKernel;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

/// <summary>Real PostgreSQL migrations, filters, quote/order persistence, capacity probing and job creation.
/// Only CuraEngine output is substituted so the test is deterministic and runs without a local slicer.</summary>
public sealed class CustomerMachineSelectionTests
{
    [Fact]
    public async Task Pinned_quote_keeps_price_profile_constraints_and_model_through_order_and_job_creation()
    {
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        var configuration = new ConfigurationBuilder().Build();
        new ServiceCollection().AddApplicationModules(configuration);
        await using var db = new PrintGridDbContext(new DbContextOptionsBuilder<PrintGridDbContext>()
            .UseNpgsql(postgres.GetConnectionString(), pg => pg.MigrationsHistoryTable("__ef_migrations_history", "shared")).Options);
        await db.Database.MigrateAsync();
        var customer = Customer.Register("selection@printgrid.test", "hash", "Selection", null);
        customer.VerifyEmail();
        var product = Model.Create(customer.Id, "Part", null, "part.3mf", "3MF", 1000, null, "models/part.3mf", "hash");
        product.ApplyGeometry(120, 80, 160, 500, 60, 10, true, true, true); product.Approve(null, DateTime.UtcNow);
        var labA = Lab.Onboard("Private A", "HCM", 1); var labB = Lab.Onboard("Private B", "HCM", 1);
        foreach (var lab in new[] { labA, labB })
            foreach (var color in new[] { "BLUE", "WHITE", "SIGNALRED" }) lab.SetMaterialStock("PLA", color, 100000);
        var reference = AddMachine(labA, "Reference");
        var pinned = AddMachine(labA, "Model X"); AddMachine(labB, "Model X"); AddMachine(labB, "Model X");
        foreach (var machine in labA.Machines.Concat(labB.Machines))
        {
            var profile = CuraProfile.Import("Standard", "Maker", machine.Model, "PLA", "profile-v1", "5.11.0",
                .4m, 1.75m, 1, 3, 300, 300, 300, true, 205, 60, "{}");
            machine.SetMatchedProfiles([profile.Id], true); machine.ReviewProfile(true, Guid.NewGuid(), null);
            db.Add(profile);
        }
        db.AddRange(customer, product, labA, labB); await db.SaveChangesAsync();
        var filter = new EfMachineFilterService(db);
        var modelId = MachineModelIdentity.For("Maker", "Model X");
        var clock = Substitute.For<IDateTimeProvider>(); clock.UtcNow.Returns(DateTime.UtcNow); clock.Today.Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        var unit = Substitute.For<IUnitOfWork>(); unit.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(call => db.SaveChangesAsync(call.Arg<CancellationToken>()));
        var slicer = Substitute.For<IQuoteSlicer>();
        var machines = labA.Machines.Concat(labB.Machines).ToList();
        slicer.GetConfiguredMachineIdsAsync(Arg.Any<CancellationToken>()).Returns(machines.Select(m => m.Id).ToList());
        slicer.SliceAsync(Arg.Any<QuoteSliceRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<QuoteSliceRequest>(); var isReference = request.MachineId == reference.Id;
            var grams = isReference ? 560 : 540;
            return new QuoteSliceResult([new("BLUE", grams), new("WHITE", grams), new("SIGNALRED", grams)],
                (isReference ? 65 : 58) * 60, PlateCount: 2, ProfileId: request.ProfileId, ProfileVersion: "profile-v1");
        });
        var rates = PricingParameterSet.Active with { PricePerPrintHour = 6000, FailureRate = .08m, PolishingPerUnit = 30000,
            ColorPricePerKg = new Dictionary<string, decimal> { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000, ["PLA:SIGNALRED"] = 310000 },
            ReferenceProfileIds = new Dictionary<string, Guid> { ["PLA:0.2"] = reference.CuraProfileIds.Single() } };
        var policy = Substitute.For<ICommercialPolicyProvider>();
        policy.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new CommercialPolicy(rates, 0, 48, new LabScoreParameters()));
        var probe = new ProductionCapacityProbe(db);
        var quoteRepo = new QuoteRepository(db);
        var handler = new CreateQuoteCommandHandler(new ModelRepository(db), quoteRepo, unit, clock, probe, filter, policy, slicer);
        var result = await handler.Handle(new(customer.Id, product.Id, "PLA", "BLUE", .2m, 30, 5, .2m,
            "ORIGINAL", ["BLUE", "WHITE", "SIGNALRED"], true, MaxColors: 3, EnclosedChamber: true,
            NozzleDiameterMm: .4m, BindingType: 1, PinnedModelId: modelId), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsPinnedModelQuote.Should().BeTrue();
        result.Value.Items.Single().Calculation!.Requirements!.PinnedModelId.Should().Be(modelId);
        var quote = (await quoteRepo.GetByIdAsync(result.Value.Id))!;
        quote.Items.Single().ProfileId.Should().NotBeNull();
        quote.Items.Single().ProfileId!.Value.Should().NotBe(quote.Items.Single().QuotedMachineId!.Value);
        quote.Items.Single().ProfileVersion.Should().Be("profile-v1");
        quote.SubmitForReview().IsSuccess.Should().BeTrue();
        quote.Approve(Guid.NewGuid(), quote.TotalPrice.Amount, quote.PromisedDeliveryDate, null, clock.UtcNow,
            TimeSpan.FromHours(48), false, 0).IsSuccess.Should().BeTrue();
        var order = Order.CreateFromQuote(quote, Address.Create("1 A", "B", "C", "HCM", ""), "PG-SELECTION", 50);
        db.Add(order); await db.SaveChangesAsync();
        var frozenMachine = await db.Set<Machine>().SingleAsync(m => m.Id == quote.Items.Single().QuotedMachineId);
        frozenMachine.SetStatus(MachineStatus.Offline); await db.SaveChangesAsync();
        var delivery = await new OrderDeliveryService(probe, clock, configuration).AssessAsync(quote, clock.Today.AddDays(30), default);
        delivery.IsSuccess.Should().BeTrue("another machine of the same model remains available");
        order.ApplyPayment(order.DepositAmount).IsSuccess.Should().BeTrue();
        var confirmed = order.DomainEvents.OfType<OrderConfirmedEvent>().Single();
        await new OrderConfirmedEventHandler(new JobRepository(db), unit, NullLogger<OrderConfirmedEventHandler>.Instance)
            .Handle(confirmed, default);
        db.ChangeTracker.Clear();
        var savedOrder = await db.Set<Order>().Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        savedOrder.Items.Single().PinnedModelId.Should().Be(modelId);
        var job = await db.Set<Job>().SingleAsync(j => j.OrderItemId == order.Items.Single().Id);
        job.FrozenRequirements()!.PinnedModelId.Should().Be(modelId);
        job.FrozenRequirements()!.EnclosedChamber.Should().BeTrue();
        Job.CreateUrgentReprint(job, 1).FrozenRequirements().Should().Be(job.FrozenRequirements());
        job.Split([2, 3]).Value.Should().OnlyContain(j => j.FrozenRequirements() == job.FrozenRequirements());
    }

    private static Machine AddMachine(Lab lab, string model)
    {
        var machine = Machine.Register(lab.Id, "Private printer", model, PrintTechnology.Fdm,
            BuildVolume.Create(300, 300, 300), .05m, .1m, ["PLA"]);
        machine.ConfigurePrinter(new PrinterConfiguration("Maker", 1, 3, .4m, 1.75m, true, 300, 120,
            99999, .2m, EnclosedChamber: true));
        lab.AddMachine(machine); return machine;
    }
}
