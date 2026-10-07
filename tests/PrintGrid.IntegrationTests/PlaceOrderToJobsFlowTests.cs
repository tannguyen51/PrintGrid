using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Application.EventHandlers;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

/// <summary>
/// The order→jobs seam against a real PostgreSQL with the real MediatR wiring:
/// PlaceOrderCommand (with the payment bypass from c7130e2) must leave the order Confirmed,
/// publish OrderConfirmedEvent from Order aggregate → PrintGridDbContext.SaveChanges, and the
/// Scheduling module's OrderConfirmedEventHandler must decompose it into scheduling.jobs rows
/// carrying the frozen estimates (FR-SCHED-006) and the BR-SCHED-001 back-dated internal due.
/// Before c7130e2 nothing called ConfirmPayment outside tests, so this chain had never run.
/// </summary>
public class PlaceOrderToJobsFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // UseSetting (not just ConfigureAppConfiguration) so the value is in
            // builder.Configuration by the time Program.cs reads it for Hangfire at startup.
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                }));
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    [Fact]
    public async Task Placing_an_order_through_the_real_pipeline_creates_the_job_for_the_order_item()
    {
        var promised = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9));

        // Minimum viable chain via the domain aggregates: verified customer, model with real
        // geometry, one-item Ready quote (qty 2 × 180 min × 40 g, 60×60×40 mm).
        var (seededCustomerId, quoteId) = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();

            var customer = Customer.Register("orders-jobs@printgrid.test", "hash-not-a-real-password", "Flow Tester", null);
            customer.VerifyEmail();

            var model = Model.Create(customer.Id, "bracket", null, "bracket.stl", "STL", 1_048_576, null);
            model.ApplyGeometry(60m, 60m, 40m, 20m, 60, 12m, true, true, true);

            var quote = Quote.CreatePending(customer.Id, TimeSpan.FromHours(48));
            quote.AddItem(
                model.Id,
                quantity: 2,
                PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.3m),
                Money.Of(150_000m),
                estimatedPrintMinutes: 180,
                estimatedMaterialGrams: 40m,
                materialCostAmount: 90_000m,
                machineTimeCostAmount: 60_000m,
                boundingWidthMm: 60m,
                boundingDepthMm: 60m,
                boundingHeightMm: 40m);
            quote.MarkReady(promised, PricingParameterSet.Active.Version, "integration seed");

            db.AddRange(customer, model, quote);
            await db.SaveChangesAsync();

            return (customer.Id, quote.Id);
        });

        // The real command, the real handler, the real DbContext publishing to the real MediatR.
        var order = await InScope(async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            var result = await sender.Send(new PlaceOrderCommand(
                seededCustomerId,
                quoteId,
                "1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000",
                AcceptTerms: true));

            result.IsSuccess.Should().BeTrue(result.Error.Message);
            result.Value.Status.Should().Be(OrderStatus.Confirmed.ToString());

            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Order>()
                .Include(o => o.Items)
                .SingleAsync(o => o.Id == result.Value.Id);
        });

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.PaymentTransactionId.Should().Be(PlaceOrderCommandHandler.PaymentBypassTransactionId);
        var orderItemId = order.Items.Single().Id;

        // The payoff: the event consumer decomposed the confirmed order into a real
        // scheduling.jobs row with the frozen estimates.
        var job = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Job>().SingleAsync(j => j.OrderItemId == orderItemId);
        });

        job.ModelId.Should().Be(order.Items.Single().ModelId);
        job.Quantity.Should().Be(2);
        job.EstimatedPrintMinutes.Should().Be(360, "180 per-unit minutes × 2 (the frozen estimate, not a constant)");
        job.Specification.MaterialCode.Should().Be("PLA");
        job.Specification.ColorCode.Should().Be("BLACK");
        job.Specification.LayerHeightMm.Should().Be(0.2m);
        job.Specification.MaterialGrams.Should().Be(80m, "40 g per unit × 2");
        job.Specification.RequiredVolume.WidthMm.Should().Be(60m);
        job.Specification.RequiredVolume.HeightMm.Should().Be(40m);
        job.Status.Should().Be(JobStatus.Pending, "job creation does not assign anything");
        job.InternalDueDate.Should().Be(
            promised.AddDays(-OrderConfirmedEventHandler.HubAndTransitBufferDays),
            "BR-SCHED-001: internal due is the promised date minus the hub buffer");

        // And the raw table the hub queries against really holds it.
        var rawCount = await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Database
                .SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM scheduling.jobs WHERE \"OrderItemId\" = {0}",
                    orderItemId)
                .SingleAsync();
        });
        rawCount.Should().Be(1, "exactly one job per order item in scheduling.jobs");
    }
}
