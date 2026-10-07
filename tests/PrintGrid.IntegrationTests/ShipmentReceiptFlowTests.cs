using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static FluentAssertions.FluentActions;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

/// <summary>
/// Core-flow tail (hub goods-out → delivery → warranty anchor) against a real PostgreSQL, driven
/// through the application commands the controllers dispatch. Verifies the three decided rules:
/// ship needs a tracking number, the hub's "delivered" is only a handover, and the CUSTOMER's
/// receipt confirmation is what stamps the 30-day warranty anchor (<see cref="Order.DeliveredAt"/>).
/// </summary>
public class ShipmentReceiptFlowTests : IAsyncLifetime
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

    private Task<Result> Send(IRequest<Result> command) =>
        InScope(sp => sp.GetRequiredService<ISender>().Send(command));

    private async Task<Order> LoadOrder(Guid orderId) =>
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();
            return await db.Set<Order>().AsNoTracking().FirstAsync(o => o.Id == orderId);
        });

    /// <summary>Create and persist an order that has cleared quality check (the state just before shipping).</summary>
    private async Task<Guid> SeedOrderInQualityCheck()
    {
        var customerId = Guid.NewGuid();
        return await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PrintGridDbContext>();

            var quote = Quote.CreatePending(customerId, TimeSpan.FromHours(24));
            quote.AddItem(
                Guid.NewGuid(),
                quantity: 1,
                PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.3m),
                Money.Of(150_000m),
                estimatedPrintMinutes: 120,
                estimatedMaterialGrams: 40m,
                materialCostAmount: 90_000m,
                machineTimeCostAmount: 60_000m,
                boundingWidthMm: 60m,
                boundingDepthMm: 60m,
                boundingHeightMm: 40m);
            quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), PricingParameterSet.Active.Version, "test");

            var order = Order.CreateFromQuote(
                quote,
                Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000"),
                $"PG-TEST-{Guid.NewGuid():N}"[..20]);
            order.ConfirmPayment("integration");
            order.TransitionTo(OrderStatus.InProduction);
            order.TransitionTo(OrderStatus.QualityCheck);

            db.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        });
    }

    [Fact]
    public async Task Hub_ships_then_delivers_and_the_customer_confirmation_is_the_warranty_anchor()
    {
        var orderId = await SeedOrderInQualityCheck();

        // An empty tracking number is rejected by the command validator before the handler runs.
        await Awaiting(() => Send(new ShipOrderCommand(orderId, "")))
            .Should().ThrowAsync<ValidationException>();

        var shipped = await Send(new ShipOrderCommand(orderId, "VNPost-TRACK-001"));
        shipped.IsSuccess.Should().BeTrue();

        var afterShip = await LoadOrder(orderId);
        afterShip.Status.Should().Be(OrderStatus.Shipping);
        afterShip.TrackingNumber.Should().Be("VNPost-TRACK-001");
        afterShip.DeliveredAt.Should().BeNull();

        var delivered = await Send(new DeliverOrderCommand(orderId));
        delivered.IsSuccess.Should().BeTrue();

        var afterDeliver = await LoadOrder(orderId);
        afterDeliver.Status.Should().Be(OrderStatus.Delivered);
        afterDeliver.DeliveredAt.Should().BeNull(
            "the hub's handover is physical only — the 30-day clock has not started yet (rule 2)");

        var customerId = afterDeliver.CustomerId;
        var confirmed = await Send(new ConfirmOrderReceiptCommand(orderId, customerId));
        confirmed.IsSuccess.Should().BeTrue();

        var afterConfirm = await LoadOrder(orderId);
        afterConfirm.Status.Should().Be(OrderStatus.Delivered);
        afterConfirm.DeliveredAt.Should().NotBeNull(
            "the customer's receipt confirmation is the warranty anchor");
        afterConfirm.ReceiptConfirmedBy.Should().Be(customerId);
    }

    [Fact]
    public async Task The_customer_cannot_confirm_receipt_before_the_hub_has_delivered()
    {
        var orderId = await SeedOrderInQualityCheck();
        var seeded = await LoadOrder(orderId);

        var tooEarly = await Send(new ConfirmOrderReceiptCommand(orderId, seeded.CustomerId));
        tooEarly.IsFailure.Should().BeTrue();
        tooEarly.Error.Code.Should().Be("conflict");
        (await LoadOrder(orderId)).DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task Another_customer_confirming_someone_elses_order_reads_as_not_found()
    {
        var orderId = await SeedOrderInQualityCheck();
        var seeded = await LoadOrder(orderId);

        await Send(new ShipOrderCommand(orderId, "VNPost-TRACK-002"));
        await Send(new DeliverOrderCommand(orderId));

        var foreign = await Send(new ConfirmOrderReceiptCommand(orderId, Guid.NewGuid()));
        foreign.IsFailure.Should().BeTrue();
        foreign.Error.Code.Should().Be("not_found");

        // The real owner still can.
        (await Send(new ConfirmOrderReceiptCommand(orderId, seeded.CustomerId))).IsSuccess.Should().BeTrue();
    }
}
