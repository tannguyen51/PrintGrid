using MediatR;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.EventHandlers;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class ProductionStatusChangedHandlerTests
{
    [Theory]
    [InlineData("Printing")]
    [InlineData("QualityCheck")]
    [InlineData("Completed")]
    public async Task Production_update_saves_once_and_publishes_its_notification_status(string status)
    {
        var order = NewOrder();
        order.ConfirmPayment("test-payment");
        order.TransitionTo(OrderStatus.InProduction);
        var item = order.Items.Single();
        var orders = Substitute.For<IOrderRepository>();
        orders.GetByItemIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(order);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var publisher = Substitute.For<IPublisher>();
        var handler = new ProductionStatusChangedHandler(orders, unitOfWork, publisher);

        await handler.Handle(new ProductionStatusChangedIntegrationEvent(item.Id, status), CancellationToken.None);

        item.ItemStatus.Should().Be(status);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(
            Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e =>
                e.OrderId == order.Id && e.CustomerId == order.CustomerId && e.Status == status),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delayed_order_publishes_one_delayed_notification()
    {
        var order = NewOrder();
        var item = order.Items.Single();
        var orders = Substitute.For<IOrderRepository>();
        orders.GetByItemIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(order);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var publisher = Substitute.For<IPublisher>();
        var handler = new ProductionStatusChangedHandler(orders, unitOfWork, publisher);

        await handler.Handle(new OrderDelayedIntegrationEvent(item.Id), CancellationToken.None);

        order.IsDelayed.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(
            Arg.Is<OrderTimelineUpdatedIntegrationEvent>(e => e.OrderId == order.Id && e.Status == "Delayed"),
            Arg.Any<CancellationToken>());
    }

    private static Order NewOrder()
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(24));
        quote.AddItem(Guid.NewGuid(), 1, PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.3m),
            Money.Of(150_000m), 180, 40m, 90_000m, 60_000m, 60m, 60m, 40m);
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "test", "test basis");
        return Order.CreateFromQuote(quote,
            Address.Create("1 Test Street", "Ward", "District", "City", "100000"), "PG-TEST");
    }
}
