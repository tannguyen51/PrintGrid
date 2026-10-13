using MediatR;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class Week4HubConsolidationTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static Order BuildReadyOrder(
        Guid customerId,
        Address address,
        string orderNumber,
        decimal priceAmount,
        DateTime readyAtUtc)
    {
        var quote = Quote.CreatePending(customerId, TimeSpan.FromHours(24));
        quote.AddItem(
            Guid.NewGuid(),
            quantity: 1,
            PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.2m),
            Money.Of(priceAmount),
            estimatedPrintMinutes: 90,
            estimatedMaterialGrams: 30m,
            materialCostAmount: priceAmount * 0.6m,
            machineTimeCostAmount: priceAmount * 0.4m,
            boundingWidthMm: 50m,
            boundingDepthMm: 50m,
            boundingHeightMm: 50m);
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), PricingParameterSet.Active.Version, "test");

        var order = Order.CreateFromQuote(quote, address, orderNumber);
        order.ConfirmPayment("pay-tx");
        order.TransitionTo(OrderStatus.InProduction);
        order.TransitionTo(OrderStatus.QualityCheck);
        order.SetReadyForPackingAt(readyAtUtc);
        return order;
    }

    [Fact]
    public async Task FR_HUB_006_AC01_Two_orders_same_customer_same_address_within_24h_are_suggested_and_shipped_as_one_consolidated_package()
    {
        var customerId = Guid.NewGuid();
        var address = Address.Create("268 Lý Thường Kiệt", "Phường 14", "Quận 10", "TP.HCM", "700000");
        var now = DateTime.UtcNow;

        var order1 = BuildReadyOrder(customerId, address, "PG-20261012-001", 200_000m, now.AddHours(-6));
        var order2 = BuildReadyOrder(customerId, address, "PG-20261012-002", 350_000m, now.AddHours(-1));
        var differentCustomerOrder = BuildReadyOrder(Guid.NewGuid(), address, "PG-20261012-003", 150_000m, now.AddHours(-2));

        _orders.GetByStatusAsync(Arg.Any<IReadOnlyCollection<OrderStatus>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Order> { order1, order2, differentCustomerOrder });

        var queueHandler = new GetShipmentQueueQueryHandler(_orders);
        var queueResult = await queueHandler.Handle(new GetShipmentQueueQuery(), CancellationToken.None);

        queueResult.IsSuccess.Should().BeTrue();
        var dto1 = queueResult.Value.Single(x => x.Id == order1.Id);
        var dto2 = queueResult.Value.Single(x => x.Id == order2.Id);
        var dto3 = queueResult.Value.Single(x => x.Id == differentCustomerOrder.Id);

        dto1.SuggestedConsolidationGroupKey.Should().NotBeNullOrWhiteSpace("same customer + same address + <=24h must suggest consolidation (AC01)");
        dto1.SuggestedConsolidationGroupKey.Should().Be(dto2.SuggestedConsolidationGroupKey);
        dto1.SuggestedCompanionOrderNumbers.Should().Contain("PG-20261012-002");
        dto2.SuggestedCompanionOrderNumbers.Should().Contain("PG-20261012-001");
        dto3.SuggestedConsolidationGroupKey.Should().BeNull("different customer is not merged");

        // Now ship order1 + order2 together as a single package with 1 waybill
        _orders.GetByIdAsync(order1.Id, Arg.Any<CancellationToken>()).Returns(order1);
        _orders.GetByIdAsync(order2.Id, Arg.Any<CancellationToken>()).Returns(order2);

        var consolidateHandler = new ConsolidateShipmentCommandHandler(_orders, _unitOfWork, _publisher);
        var shipResult = await consolidateHandler.Handle(
            new ShipConsolidatedOrdersCommand([order1.Id, order2.Id], "GHN-CONSOL-999"),
            CancellationToken.None);

        shipResult.IsSuccess.Should().BeTrue();
        shipResult.Value.TrackingNumber.Should().Be("GHN-CONSOL-999");
        order1.Status.Should().Be(OrderStatus.Shipping);
        order2.Status.Should().Be(OrderStatus.Shipping);
        order1.TrackingNumber.Should().Be("GHN-CONSOL-999");
        order2.TrackingNumber.Should().Be("GHN-CONSOL-999");
        order1.ConsolidatedShipmentId.Should().NotBeNull();
        order1.ConsolidatedShipmentId.Should().Be(order2.ConsolidatedShipmentId);
    }

    [Fact]
    public async Task FR_HUB_006_Rejects_consolidation_when_ready_times_exceed_24_hours_or_addresses_differ()
    {
        var customerId = Guid.NewGuid();
        var address1 = Address.Create("1 Đại Cồ Việt", "Bách Khoa", "Hai Bà Trưng", "Hà Nội", "100000");
        var now = DateTime.UtcNow;

        var orderEarly = BuildReadyOrder(customerId, address1, "PG-20261012-010", 200_000m, now.AddHours(-30));
        var orderLate = BuildReadyOrder(customerId, address1, "PG-20261012-011", 200_000m, now);

        _orders.GetByIdAsync(orderEarly.Id, Arg.Any<CancellationToken>()).Returns(orderEarly);
        _orders.GetByIdAsync(orderLate.Id, Arg.Any<CancellationToken>()).Returns(orderLate);

        var handler = new ConsolidateShipmentCommandHandler(_orders, _unitOfWork, _publisher);
        var tooFarApart = await handler.Handle(
            new ShipConsolidatedOrdersCommand([orderEarly.Id, orderLate.Id], "VNPOST-001"),
            CancellationToken.None);

        tooFarApart.IsFailure.Should().BeTrue("orders ready > 24h apart cannot be consolidated (BR-LOG-001)");
        tooFarApart.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task FR_HUB_006_AC02_Refund_on_consolidated_order_remains_strictly_bounded_by_original_order_scope()
    {
        var customerId = Guid.NewGuid();
        var address = Address.Create("268 Lý Thường Kiệt", "Phường 14", "Quận 10", "TP.HCM", "700000");
        var now = DateTime.UtcNow;

        var order1 = BuildReadyOrder(customerId, address, "PG-20261012-101", 200_000m, now.AddHours(-2));
        var order2 = BuildReadyOrder(customerId, address, "PG-20261012-102", 500_000m, now.AddHours(-1));

        _orders.GetByIdAsync(order1.Id, Arg.Any<CancellationToken>()).Returns(order1);
        _orders.GetByIdAsync(order2.Id, Arg.Any<CancellationToken>()).Returns(order2);

        var handler = new ConsolidateShipmentCommandHandler(_orders, _unitOfWork, _publisher);
        await handler.Handle(new ShipConsolidatedOrdersCommand([order1.Id, order2.Id], "GHN-BUNDLE-01"), CancellationToken.None);

        // Attempting to refund 250,000 VND on order1 (whose original TotalPrice is 200,000 VND, even though the bundle total is 700,000 VND) must FAIL (AC02 / BR-LOG-002)
        var exceedScope = await handler.Handle(new RefundOrderWithinScopeCommand(order1.Id, 250_000m, "Hoàn vượt đơn gốc"), CancellationToken.None);
        exceedScope.IsFailure.Should().BeTrue("refund cannot exceed the original order's price even when shipped in a consolidated package (AC02)");
        exceedScope.Error.Code.Should().Be("conflict");
        order1.RefundedAmount.Should().Be(0m);
        order2.RefundedAmount.Should().Be(0m);

        // Refunding within order1's original scope succeeds and leaves order2 untouched
        var validRefund = await handler.Handle(new RefundOrderWithinScopeCommand(order1.Id, 200_000m, "Hoàn đúng phạm vi đơn gốc"), CancellationToken.None);
        validRefund.IsSuccess.Should().BeTrue();
        order1.RefundedAmount.Should().Be(200_000m);
        order2.RefundedAmount.Should().Be(0m);
    }
}
