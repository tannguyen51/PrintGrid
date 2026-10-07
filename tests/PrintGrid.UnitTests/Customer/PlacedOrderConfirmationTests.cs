using FluentAssertions;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

/// <summary>
/// Pins the order→jobs seam at the payment-bypass commit (c7130e2, 07/10): placing an
/// order must CONFIRM it immediately (the payment lane is deferred) and raise exactly one
/// OrderConfirmedEvent carrying the frozen quote estimates, or no job is ever created.
///
/// Two wiring traps this file deliberately walks around — both were suspects for the
/// earlier failed attempt of this test, which saw "not_found — Quote with id ..." even
/// with a stubbed GetByIdAsync:
///  1. PlaceOrderCommand is (CustomerId, QuoteId, ...) — Customer FIRST. Both are Guid,
///     so swapping them compiles silently and makes the handler look up the customer id
///     as if it were the quote id.
///  2. An NSubstitute substitute that is stubbed but never handed to the constructor is
///     indistinguishable from one that was never stubbed: the fresh substitute the handler
///     actually holds auto-returns a completed Task&lt;Quote?&gt; whose result is null →
///     not_found, no matter how the other instance is configured (Arg.Any included).
///     The substitute stubbed here is the same instance the handler receives — that is
///     the wiring regression this test protects.
/// </summary>
public class PlacedOrderConfirmationTests
{
    [Fact]
    public async Task Placing_an_order_confirms_it_instead_of_leaving_it_payment_pending()
    {
        var (_, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        result.Value.Status.Should().Be(OrderStatus.Confirmed.ToString());

        placed!.Status.Should().Be(OrderStatus.Confirmed);
        placed.Status.Should().NotBe(OrderStatus.PaymentPending,
            "the payment bypass (c7130e2) must run straight after Order.CreateFromQuote, " +
            "otherwise the fulfilment chain is never reached");
    }

    [Fact]
    public async Task Placing_an_order_records_the_bypass_sentinel_in_PaymentTransactionId()
    {
        var (_, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        placed!.PaymentTransactionId.Should().Be(PlaceOrderCommandHandler.PaymentBypassTransactionId);
        placed.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Placing_an_order_raises_exactly_one_OrderConfirmedEvent_with_the_frozen_estimates()
    {
        var (customerId, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        placed!.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent,
            "ConfirmPayment raises the one event the Scheduling module turns into jobs");

        var evt = placed.DomainEvents.OfType<OrderConfirmedEvent>().Single();
        evt.OrderId.Should().Be(placed.Id);
        evt.CustomerId.Should().Be(customerId);
        evt.PromisedDeliveryDate.Should().Be(placed.PromisedDeliveryDate);

        var item = evt.Items.Should().ContainSingle().Subject;
        item.OrderItemId.Should().Be(placed.Items.Single().Id);
        item.Quantity.Should().Be(2);
        item.MaterialCode.Should().Be("PLA");
        item.ColorCode.Should().Be("BLACK");
        item.EstimatedPrintMinutes.Should().Be(180);
        item.EstimatedMaterialGrams.Should().Be(40m);
        item.BoundingWidthMm.Should().Be(60m);
        item.BoundingDepthMm.Should().Be(60m);
        item.BoundingHeightMm.Should().Be(40m);
    }

    [Fact]
    public async Task Placing_an_order_persists_exactly_once_through_the_unit_of_work()
    {
        var orders = Substitute.For<IOrderRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var (_, _, _, result, _) = await PlaceWithQuote(orders, uow);

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        await orders.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static async Task<(Guid CustomerId, Quote Quote, DateOnly Promised, Result<OrderDto> Result, Order? Placed)> PlaceWithQuote(
        IOrderRepository? orders = null, IUnitOfWork? uow = null)
    {
        var customerId = Guid.NewGuid();
        var promised = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9));
        var quote = ReadyQuote(customerId, promised);

        var quotes = Substitute.For<IQuoteRepository>();
        quotes.GetByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        orders ??= Substitute.For<IOrderRepository>();
        Order? placed = null;
        _ = orders.AddAsync(Arg.Do<Order>(o => placed = o), Arg.Any<CancellationToken>());
        orders.CountAsync(Arg.Any<CancellationToken>()).Returns(0);

        uow ??= Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        clock.Today.Returns(DateOnly.FromDateTime(DateTime.UtcNow));

        var handler = new PlaceOrderCommandHandler(quotes, orders, uow, clock);

        // (CustomerId, QuoteId, ...) — not QuoteId first; see class comment, trap 1.
        var result = await handler.Handle(
            new PlaceOrderCommand(
                customerId,
                quote.Id,
                "1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000",
                AcceptTerms: true),
            CancellationToken.None);

        return (customerId, quote, promised, result, placed);
    }

    private static Quote ReadyQuote(Guid customerId, DateOnly promised)
    {
        var quote = Quote.CreatePending(customerId, TimeSpan.FromHours(48));
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
        quote.MarkReady(promised, PricingParameterSet.Active.Version, "unit basis");
        return quote;
    }
}
