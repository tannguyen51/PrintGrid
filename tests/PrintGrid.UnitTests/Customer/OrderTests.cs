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
            estimatedMaterialGrams: 40m);
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        return Order.CreateFromQuote(quote, address, "PG-20260902-00001");
    }
}
