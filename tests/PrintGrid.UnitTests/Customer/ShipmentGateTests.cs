using MediatR;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Application.EventHandlers;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

/// <summary>
/// Goods-out gate (ship only when every item passed QC as "Completed" and the order was not fully
/// refunded) plus the item-level "Completed" production signal.
/// </summary>
public class ShipmentGateTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    [Fact]
    public async Task Ship_is_blocked_while_any_item_is_still_Pending()
    {
        var order = BuildQualityCheckOrder("Completed", "Pending");
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await Handler().Handle(new ShipOrderCommand(order.Id, "TRACK-001"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        result.Error.Message.Should().Contain("Pending");
        order.Status.Should().Be(OrderStatus.QualityCheck, "a blocked ship must not move the order");
    }

    [Fact]
    public async Task Ship_is_blocked_while_any_item_is_only_in_QualityCheck()
    {
        var order = BuildQualityCheckOrder("Completed", "QualityCheck");
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await Handler().Handle(new ShipOrderCommand(order.Id, "TRACK-002"), CancellationToken.None);

        result.IsFailure.Should().BeTrue("QualityCheck is a request for inspection, not a completed QC gate");
        result.Error.Code.Should().Be("conflict");
        result.Error.Message.Should().Contain("QualityCheck");
        order.Status.Should().Be(OrderStatus.QualityCheck);
    }

    [Fact]
    public async Task Ship_succeeds_when_every_item_is_Completed()
    {
        var order = BuildQualityCheckOrder("Completed", "Completed");
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await Handler().Handle(new ShipOrderCommand(order.Id, " TRACK-003 "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        order.Status.Should().Be(OrderStatus.Shipping);
        order.TrackingNumber.Should().Be("TRACK-003");
    }

    [Fact]
    public async Task Ship_is_blocked_when_the_order_was_fully_refunded()
    {
        var order = BuildQualityCheckOrder("Completed", "Completed");
        order.RefundWithinOriginalOrderScope(order.PaidAmount).IsSuccess.Should().BeTrue();
        order.IsFullyRefunded.Should().BeTrue();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await Handler().Handle(new ShipOrderCommand(order.Id, "TRACK-004"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        result.Error.Message.Should().Contain("hoàn tiền toàn phần");
        order.Status.Should().Be(OrderStatus.QualityCheck);
    }

    [Fact]
    public async Task Consolidated_ship_is_blocked_when_one_member_has_an_item_not_Completed()
    {
        var customerId = Guid.NewGuid();
        var readyAt = DateTime.UtcNow.AddHours(-1);
        var order1 = BuildQualityCheckOrder("Completed", "Completed", customerId, readyAt);
        var order2 = BuildQualityCheckOrder("Completed", "QualityCheck", customerId, readyAt);
        _orders.GetByIdAsync(order1.Id, Arg.Any<CancellationToken>()).Returns(order1);
        _orders.GetByIdAsync(order2.Id, Arg.Any<CancellationToken>()).Returns(order2);

        var handler = new ConsolidateShipmentCommandHandler(_orders, _unitOfWork, _publisher);
        var result = await handler.Handle(
            new ShipConsolidatedOrdersCommand([order1.Id, order2.Id], "GHN-CONSOL-001"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        result.Error.Message.Should().Contain("QualityCheck");
        order1.Status.Should().Be(OrderStatus.QualityCheck);
        order2.Status.Should().Be(OrderStatus.QualityCheck);
    }

    [Fact]
    public async Task Completed_production_signal_sets_the_item_status_without_moving_the_order()
    {
        var order = BuildProductionOrder();
        var item = order.Items.First();
        _orders.GetByItemIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ProductionStatusChangedHandler(_orders, _unitOfWork, _publisher);
        await handler.Handle(new ProductionStatusChangedIntegrationEvent(item.Id, "Completed"), CancellationToken.None);

        item.ItemStatus.Should().Be("Completed");
        order.Status.Should().Be(OrderStatus.InProduction,
            "Completed is an item-level signal; the order must not transition on it");
    }

    [Fact]
    public async Task Completed_signal_keeps_a_quality_check_order_in_quality_check()
    {
        var order = BuildQualityCheckOrder("Completed", "QualityCheck");
        var lastItem = order.Items.ElementAt(1);
        _orders.GetByItemIdAsync(lastItem.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ProductionStatusChangedHandler(_orders, _unitOfWork, _publisher);
        await handler.Handle(new ProductionStatusChangedIntegrationEvent(lastItem.Id, "Completed"), CancellationToken.None);

        order.Items.Should().OnlyContain(i => i.ItemStatus == "Completed");
        order.Status.Should().Be(OrderStatus.QualityCheck,
            "a ready order waits for the hub to ship it; Completed must not move it");
    }

    private ShipOrderCommandHandler Handler() => new(_orders, _unitOfWork, _publisher);

    private static Order BuildProductionOrder()
    {
        var order = BuildOrder(Guid.NewGuid(), ("Pending", 0));
        order.ApplyPayment(order.TotalPrice.Amount, "pay");
        order.TransitionTo(OrderStatus.InProduction);
        return order;
    }

    private static Order BuildQualityCheckOrder(
        string firstStatus,
        string secondStatus,
        Guid? customerId = null,
        DateTime? readyAtUtc = null)
    {
        var order = BuildOrder(customerId ?? Guid.NewGuid(), (firstStatus, 0), (secondStatus, 1));
        order.ApplyPayment(order.TotalPrice.Amount, "pay");
        order.TransitionTo(OrderStatus.InProduction);
        order.TransitionTo(OrderStatus.QualityCheck);
        if (readyAtUtc is not null) order.SetReadyForPackingAt(readyAtUtc.Value);
        return order;
    }

    private static Order BuildOrder(Guid customerId, params (string ItemStatus, int Index)[] statuses)
    {
        var quote = Quote.CreatePending(customerId, TimeSpan.FromHours(24));
        for (var i = 0; i < 2; i++)
        {
            quote.AddItem(
                Guid.NewGuid(),
                quantity: 1,
                PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.2m),
                Money.Of(150_000m),
                estimatedPrintMinutes: 90,
                estimatedMaterialGrams: 30m,
                materialCostAmount: 90_000m,
                machineTimeCostAmount: 60_000m,
                boundingWidthMm: 50m,
                boundingDepthMm: 50m,
                boundingHeightMm: 50m);
        }
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), PricingParameterSet.Active.Version, "test");

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        var order = Order.CreateFromQuote(quote, address, $"PG-TEST-{Guid.NewGuid():N}"[..20]);

        foreach (var (status, index) in statuses)
            order.Items.ElementAt(index).UpdateStatus(status);

        return order;
    }
}
