using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PrintGrid.Api.BackgroundServices;
using PrintGrid.Api.Controllers;
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

namespace PrintGrid.OrderWorkflowTests;

public sealed class CodConfirmationTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Rejecting_a_ready_order_cancels_successfully_and_keeps_the_deposit_ledger()
    {
        var order = ReadyOrder();
        order.ConfirmCodReceipt(false, order.CustomerId, Now.AddHours(1)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaidAmount.Should().Be(100_000m);
        order.DepositAmount.Should().Be(100_000m);
        order.RefundedAmount.Should().Be(0m);
        order.IsCodConfirmed.Should().BeFalse();
        order.ConfirmCodReceipt(false, order.CustomerId, Now.AddHours(1)).IsSuccess.Should().BeTrue();
        order.Ship("DO-NOT-SHIP").IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void Server_enforces_the_deadline_including_the_exact_boundary(int milliseconds, bool allowed)
    {
        var order = ReadyOrder();
        var result = order.ConfirmCodReceipt(true, order.CustomerId, Now.AddHours(24).AddMilliseconds(milliseconds));
        result.IsSuccess.Should().Be(allowed);
        order.IsCodConfirmed.Should().Be(allowed);
        order.Status.Should().Be(OrderStatus.QualityCheck);
    }

    [Fact]
    public void Confirmation_requests_are_idempotent_and_never_extend_an_existing_deadline()
    {
        var order = ReadyOrder();
        order.RequestCodConfirmation(Now.AddHours(23)).IsSuccess.Should().BeTrue();
        order.RequestCodConfirmation(Now.AddHours(25)).IsSuccess.Should().BeTrue();
        order.CodConfirmationRequestedAtUtc.Should().Be(Now);
        order.CodConfirmationDeadlineUtc.Should().Be(Now.AddHours(24));
        order.CodConfirmationRequestCount.Should().Be(1);
        order.ConfirmCodReceipt(true, order.CustomerId, Now.AddHours(25)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Expired_ready_orders_can_cancel_but_confirmed_and_shipped_orders_cannot_timeout()
    {
        var order = ReadyOrder();
        order.CancelDueToCodTimeout(Now.AddHours(24).AddMilliseconds(-1)).IsFailure.Should().BeTrue();
        order.CancelDueToCodTimeout(Now.AddHours(24)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaidAmount.Should().Be(100_000m);
        order.RefundedAmount.Should().Be(0m);
        order.IsCodConfirmationRequired(Now).Should().BeFalse();

        var confirmed = ReadyOrder();
        confirmed.ConfirmCodReceipt(true, confirmed.CustomerId, Now.AddHours(1));
        confirmed.CancelDueToCodTimeout(Now.AddHours(25)).IsFailure.Should().BeTrue();
        var shipped = ReadyOrder();
        shipped.Ship("TRACK-001").IsSuccess.Should().BeTrue();
        shipped.CancelDueToCodTimeout(Now.AddHours(25)).IsFailure.Should().BeTrue();
        shipped.ConfirmCodReceipt(true, shipped.CustomerId, Now.AddHours(1)).IsFailure.Should().BeTrue();
        shipped.ConfirmCodReceipt(false, shipped.CustomerId, Now.AddHours(1)).IsFailure.Should().BeTrue();
        shipped.Status.Should().Be(OrderStatus.Shipping);
    }

    [Fact]
    public void Orders_without_a_request_or_using_online_payment_cannot_confirm_COD()
    {
        var order = ReadyOrder(request: false);
        order.ConfirmCodReceipt(true, order.CustomerId, Now).IsFailure.Should().BeTrue();
        order.IsCodConfirmationRequired(Now).Should().BeFalse();
        typeof(Order).GetProperty(nameof(Order.PaymentMethod))!.SetValue(order, "Online");
        order.RequestCodConfirmation(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Only_the_owner_can_make_a_COD_decision_and_replays_publish_no_duplicate_events()
    {
        var order = ReadyOrder();
        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var unit = Substitute.For<IUnitOfWork>();
        var publisher = Substitute.For<IPublisher>();
        var handler = new ConfirmCodReceiptCommandHandler(repository, unit, publisher);
        (await handler.Handle(new ConfirmCodReceiptCommand(order.Id, false, Guid.NewGuid()), default))
            .IsFailure.Should().BeTrue();
        (await handler.Handle(new ConfirmCodReceiptCommand(order.Id, true), default)).IsFailure.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.QualityCheck);
        await unit.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        var command = new ConfirmCodReceiptCommand(order.Id, false, order.CustomerId);
        (await handler.Handle(command, default)).IsSuccess.Should().BeTrue();
        (await handler.Handle(command, default)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        await unit.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.Status == "CodRejected"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replayed_request_commands_do_not_duplicate_the_request_or_customer_notification()
    {
        var order = ReadyOrder(request: false);
        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var unit = Substitute.For<IUnitOfWork>();
        var publisher = Substitute.For<IPublisher>();
        var handler = new RequestCodConfirmationCommandHandler(repository, unit, publisher);
        (await handler.Handle(new RequestCodConfirmationCommand(order.Id), default)).IsSuccess.Should().BeTrue();
        var deadline = order.CodConfirmationDeadlineUtc;
        (await handler.Handle(new RequestCodConfirmationCommand(order.Id), default)).IsSuccess.Should().BeTrue();
        order.CodConfirmationDeadlineUtc.Should().Be(deadline);
        order.CodConfirmationRequestCount.Should().Be(1);
        await unit.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.Status == "CodConfirmationRequested"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deployed_processor_cancels_persisted_expired_orders_once_and_preserves_others()
    {
        await using var services = Services(out var publisher);
        var expired = ReadyOrder(Now.AddHours(-25));
        var confirmed = ReadyOrder(Now.AddHours(-25));
        confirmed.ConfirmCodReceipt(true, confirmed.CustomerId, Now.AddHours(-24)).IsSuccess.Should().BeTrue();
        var future = ReadyOrder(Now);
        var shipped = ReadyOrder(Now.AddHours(-25));
        shipped.Ship("ALREADY-SHIPPED");
        await Seed(services, expired, confirmed, future, shipped);
        await Run(services, Now);
        await Run(services, Now.AddMinutes(1));
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        (await db.Set<Order>().SingleAsync(o => o.Id == expired.Id)).Status.Should().Be(OrderStatus.Cancelled);
        (await db.Set<Order>().SingleAsync(o => o.Id == expired.Id)).RefundedAmount.Should().Be(0m);
        (await db.Set<Order>().SingleAsync(o => o.Id == confirmed.Id)).Status.Should().Be(OrderStatus.QualityCheck);
        (await db.Set<Order>().SingleAsync(o => o.Id == future.Id)).Status.Should().Be(OrderStatus.QualityCheck);
        (await db.Set<Order>().SingleAsync(o => o.Id == shipped.Id)).Status.Should().Be(OrderStatus.Shipping);
        await publisher.Received(1).Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.OrderId == expired.Id && e.Status == "CodExpired"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Processor_recovers_old_ready_orders_missing_a_deadline_without_cancelling_them()
    {
        await using var services = Services(out var publisher);
        var legacy = ReadyOrder(request: false);
        await Seed(services, legacy);
        await Run(services, Now);
        await Run(services, Now.AddMinutes(1));
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        var stored = await db.Set<Order>().SingleAsync();
        stored.Status.Should().Be(OrderStatus.QualityCheck);
        stored.CodConfirmationDeadlineUtc.Should().BeAfter(Now.AddHours(23));
        stored.CodConfirmationRequestCount.Should().Be(1);
        await publisher.Received(1).Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.Status == "CodConfirmationRequested"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reminder_is_sent_once_per_window_and_one_order_failure_does_not_stop_the_batch()
    {
        await using var services = Services(out var publisher);
        var reminder = ReadyOrder(Now.AddHours(-23));
        var expired1 = ReadyOrder(Now.AddHours(-25));
        var expired2 = ReadyOrder(Now.AddHours(-25));
        await Seed(services, reminder, expired1, expired2);
        publisher.When(p => p.Publish(Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.OrderId == expired1.Id), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Simulate a failed notification transport"));
        await Run(services, Now);
        await Run(services, Now.AddMinutes(1));
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        (await db.Set<Order>().SingleAsync(o => o.Id == expired2.Id)).Status.Should().Be(OrderStatus.Cancelled);
        (await db.UserNotifications.CountAsync(n => n.UserId == reminder.CustomerId && n.Title.Contains("sắp hết hạn"))).Should().Be(1);
    }

    [Fact]
    public async Task A_large_reminder_queue_does_not_leave_later_orders_unprocessed()
    {
        await using var services = Services(out _);
        var orders = Enumerable.Range(0, 201).Select(_ => ReadyOrder(Now.AddHours(-23))).ToArray();
        await Seed(services, orders);
        await Run(services, Now);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        (await db.UserNotifications.CountAsync(n => n.Title.Contains("sắp hết hạn"))).Should().Be(201);
    }

    [Fact]
    public void Manual_request_permissions_do_not_require_a_Hub_user_to_also_be_a_customer()
    {
        typeof(OrdersController).GetCustomAttributes<AuthorizeAttribute>().Should().OnlyContain(a => a.Policy == null);
        var methods = typeof(OrdersController).GetMethods().Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
        foreach (var method in methods)
        {
            var auth = method.GetCustomAttributes<AuthorizeAttribute>().ToList();
            auth.Should().NotBeEmpty();
            if (method.Name == nameof(OrdersController.RequestCodConfirmation))
                auth.Should().Contain(a => a.Roles != null && a.Roles.Contains("HubFulfillment") && a.Roles.Contains("OpsManager"));
            else auth.Should().Contain(a => a.Policy == "RequireCustomer");
        }
    }

    private static Order ReadyOrder(DateTime? requestedAt = null, bool request = true)
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), 1, PrintConfiguration.Create("PLA", "BLACK", .2m, 20, .3m), Money.Of(200_000m), 30, 10, 100_000, 100_000, 20, 20, 20);
        quote.MarkReady(DateOnly.FromDateTime(Now.AddDays(7)), "test");
        var order = Order.CreateFromQuote(quote, Address.Create("A", "B", "C", "HCM", ""), $"PG-COD-{Guid.NewGuid():N}"[..24], 50m);
        order.ApplyPayment(100_000m).IsSuccess.Should().BeTrue();
        order.TransitionTo(OrderStatus.InProduction).IsSuccess.Should().BeTrue();
        order.TransitionTo(OrderStatus.QualityCheck).IsSuccess.Should().BeTrue();
        if (request) order.RequestCodConfirmation(requestedAt ?? Now).IsSuccess.Should().BeTrue();
        return order;
    }

    private static ServiceProvider Services(out IPublisher publisher)
    {
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Customer.Infrastructure.CustomerModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Scheduling.Infrastructure.SchedulingModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Analytics.Infrastructure.AnalyticsModule).Assembly);
        ModuleAssemblyRegistry.Register(typeof(PrintGrid.Modules.Admin.Infrastructure.AdminModule).Assembly);
        var name = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<PrintGridDbContext>(o => o.UseInMemoryDatabase(name));
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(ConfirmCodReceiptCommand).Assembly));
        publisher = Substitute.For<IPublisher>();
        services.AddSingleton(publisher);
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton(NullLogger<CodConfirmationDeadlineProcessor>.Instance);
        services.AddScoped<Microsoft.Extensions.Logging.ILogger<CodConfirmationDeadlineProcessor>>(_ => NullLogger<CodConfirmationDeadlineProcessor>.Instance);
        services.AddScoped<CodConfirmationDeadlineProcessor>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
    private static async Task Seed(ServiceProvider services, params Order[] orders)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        db.AddRange(orders); await db.SaveChangesAsync();
    }
    private static async Task Run(ServiceProvider services, DateTime now)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CodConfirmationDeadlineProcessor>().RunAsync(now);
    }
}
