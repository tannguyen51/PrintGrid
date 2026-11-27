using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Api.BackgroundServices;
using PrintGrid.Api.Payments;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;
using Testcontainers.PostgreSql;

namespace PrintGrid.IntegrationTests;

public sealed class CodPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    public ServiceProvider Services { get; private set; } = null!;
    public IPublisher Publisher { get; } = Substitute.For<IPublisher>();
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Analytics.Infrastructure.AnalyticsModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Admin.Infrastructure.AdminModule).Assembly);
        var services = new ServiceCollection();
        services.AddDbContext<PrintGridDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(ConfirmCodReceiptCommand).Assembly));
        services.AddSingleton(Publisher);
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILogger<CodConfirmationDeadlineProcessor>>(_ => NullLogger<CodConfirmationDeadlineProcessor>.Instance);
        services.AddScoped<CodConfirmationDeadlineProcessor>();
        Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PrintGridDbContext>().Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (Services is not null) await Services.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

public sealed class CodPostgresTests(CodPostgresFixture fixture) : IClassFixture<CodPostgresFixture>
{
    [Fact]
    public async Task Persisted_orders_reject_cancel_and_recover_after_a_new_processor_scope()
    {
        var rejected = await Seed(DateTime.UtcNow.AddHours(-1));
        var decision = await Decide(rejected, false);
        decision.IsSuccess.Should().BeTrue();
        (await Load(rejected.Id)).Status.Should().Be(OrderStatus.Cancelled);
        (await Load(rejected.Id)).PaidAmount.Should().Be(100_000m);

        var expired = await Seed(DateTime.UtcNow.AddHours(-25));
        (await Decide(expired, true)).IsFailure.Should().BeTrue();
        await Run();
        var stored = await Load(expired.Id);
        stored.Status.Should().Be(OrderStatus.Cancelled);
        stored.RefundedAmount.Should().Be(0m);
        var legacy = await Seed(null);
        await Run();
        var firstDeadline = (await Load(legacy.Id)).CodConfirmationDeadlineUtc;
        firstDeadline.Should().NotBeNull();
        await Run();
        var afterRestart = await Load(legacy.Id);
        afterRestart.Status.Should().Be(OrderStatus.QualityCheck);
        afterRestart.CodConfirmationDeadlineUtc.Should().Be(firstDeadline);
        afterRestart.CodConfirmationRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Two_processors_cancel_the_same_expired_order_only_once()
    {
        var expired = await Seed(DateTime.UtcNow.AddHours(-25));
        await Task.WhenAll(Run(), Run());
        (await Load(expired.Id)).Status.Should().Be(OrderStatus.Cancelled);
        await fixture.Publisher.Received(1).Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e =>
            e.OrderId == expired.Id && e.Status == "CodExpired"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_processor_waits_for_an_in_flight_confirmation_and_cannot_cancel_it()
    {
        var order = await Seed(DateTime.UtcNow.AddHours(-1));
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        await using var transaction = await CodOrderTransaction.BeginAsync(db, order.Id, default);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new ConfirmCodReceiptCommand(order.Id, true, order.CustomerId));
        result.IsSuccess.Should().BeTrue();
        // The other instance sees the committed, still-unconfirmed version, but must wait for its row lock.
        var processor = Run(DateTime.UtcNow.AddHours(25));
        await Task.Delay(150);
        processor.IsCompleted.Should().BeFalse();
        await transaction!.CommitAsync();
        await processor.WaitAsync(TimeSpan.FromSeconds(30));
        var stored = await Load(order.Id);
        stored.IsCodConfirmed.Should().BeTrue();
        stored.Status.Should().Be(OrderStatus.QualityCheck);
        await fixture.Publisher.DidNotReceive().Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e =>
            e.OrderId == order.Id && e.Status == "CodExpired"), Arg.Any<CancellationToken>());
    }

    private async Task<PrintGrid.SharedKernel.Results.Result> Decide(Order order, bool accepted)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        await using var transaction = await CodOrderTransaction.BeginAsync(db, order.Id, default);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new ConfirmCodReceiptCommand(order.Id, accepted, order.CustomerId));
        if (result.IsSuccess) await transaction!.CommitAsync();
        return result;
    }
    private async Task Run(DateTime? now = null)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CodConfirmationDeadlineProcessor>().RunAsync(now);
    }
    private async Task<Order> Load(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PrintGridDbContext>().Set<Order>().AsNoTracking().SingleAsync(o => o.Id == id);
    }
    private async Task<Order> Seed(DateTime? requestAt)
    {
        var now = DateTime.UtcNow;
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), 1, PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m),
            Money.Of(200_000m), 30, 10, 100_000m, 100_000m, 20, 20, 20);
        quote.MarkReady(DateOnly.FromDateTime(now.AddDays(7)), "test");
        var order = Order.CreateFromQuote(quote, Address.Create("A", "B", "C", "HCM", ""), $"PG-COD-{Guid.NewGuid():N}"[..24], 50m);
        order.ApplyPayment(100_000m).IsSuccess.Should().BeTrue();
        order.TransitionTo(OrderStatus.InProduction).IsSuccess.Should().BeTrue();
        order.TransitionTo(OrderStatus.QualityCheck).IsSuccess.Should().BeTrue();
        if (requestAt.HasValue) order.RequestCodConfirmation(requestAt.Value).IsSuccess.Should().BeTrue();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        db.Add(order); await db.SaveChangesAsync();
        return order;
    }
}
