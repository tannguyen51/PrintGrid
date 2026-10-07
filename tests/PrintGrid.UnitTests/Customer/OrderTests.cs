using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class OrderTests
{
    [Fact]
    public void ConfirmPayment_moves_a_pending_order_to_confirmed_and_raises_the_event()
    {
        var order = OrderFromReadyQuote();

        var result = order.ConfirmPayment("txn_123");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void ConfirmPayment_is_rejected_the_second_time()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");

        var result = order.ConfirmPayment("txn_456");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void Order_cannot_skip_straight_from_confirmed_to_shipping()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");

        var result = order.TransitionTo(OrderStatus.Shipping);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failed_inspection_can_send_a_quality_check_order_back_into_production()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");
        order.TransitionTo(OrderStatus.InProduction);
        order.TransitionTo(OrderStatus.QualityCheck);

        var result = order.TransitionTo(OrderStatus.InProduction);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.InProduction);
    }

    [Fact]
    public void Hub_delivery_does_NOT_start_the_guarantee_clock()
    {
        var order = OrderInQualityCheck();

        var moved = order.TransitionTo(OrderStatus.Shipping);
        moved.IsSuccess.Should().BeTrue();
        var delivered = order.TransitionTo(OrderStatus.Delivered);

        delivered.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        // Rule 2: the physical handover must NOT stamp the 30-day warranty anchor.
        order.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Ship_requires_a_tracking_number()
    {
        var order = OrderInQualityCheck();

        var blank = order.Ship("   ");
        blank.IsFailure.Should().BeTrue();
        blank.Error.Code.Should().Be("validation_error");
        order.Status.Should().Be(OrderStatus.QualityCheck, "a rejected ship must not move the order");

        var none = order.Ship(null);
        none.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Ship_moves_a_quality_checked_order_to_shipping_and_stores_the_tracking()
    {
        var order = OrderInQualityCheck();

        var result = order.Ship(" VNPost-12345 ");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipping);
        order.TrackingNumber.Should().Be("VNPost-12345");
        order.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Ship_is_rejected_before_the_order_is_quality_checked()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");

        var result = order.Ship("VNPost-12345");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Deliver_requires_the_order_to_be_shipped_first()
    {
        var order = OrderInQualityCheck();

        var tooEarly = order.MarkDelivered();
        tooEarly.IsFailure.Should().BeTrue();
        tooEarly.Error.Code.Should().Be("conflict");

        order.Ship("VNPost-12345");
        var delivered = order.MarkDelivered();

        delivered.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Customer_confirmation_requires_the_order_to_be_delivered()
    {
        var order = OrderInQualityCheck();
        order.Ship("VNPost-12345");

        var result = order.ConfirmReceipt(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        order.Status.Should().Be(OrderStatus.Shipping);
        order.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Customer_confirmation_stamps_the_guarantee_anchor_and_who_confirmed()
    {
        var order = OrderInQualityCheck();
        order.Ship("VNPost-12345");
        order.MarkDelivered();

        var customerId = Guid.NewGuid();
        var result = order.ConfirmReceipt(customerId);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
        order.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        order.ReceiptConfirmedBy.Should().Be(customerId);
    }

    private static Order OrderInQualityCheck()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");
        order.TransitionTo(OrderStatus.InProduction);
        order.TransitionTo(OrderStatus.QualityCheck);
        return order;
    }

    [Fact]
    public void Order_copies_every_quote_item_with_its_configuration()
    {
        var order = OrderFromReadyQuote();

        order.Items.Should().HaveCount(1);
        order.Items.Single().Configuration.MaterialCode.Should().Be("PLA");
        order.Items.Single().LineTotal.Amount.Should().Be(300_000m);
    }

    private static Order OrderFromReadyQuote()
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(24));
        quote.AddItem(
            Guid.NewGuid(),
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
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), PricingParameterSet.Active.Version, "test basis");

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        return Order.CreateFromQuote(quote, address, "PG-20260902-00001");
    }
}
