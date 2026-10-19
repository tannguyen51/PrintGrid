using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
/// Pins the order→jobs seam after FR-CUST-012 removed the payment bypass: placing an order must
/// leave it PaymentPending with a deposit requirement and raise NO OrderConfirmedEvent. The event
/// (and therefore the Scheduling jobs) is raised only once the deposit is actually paid.
/// </summary>
public class PlacedOrderConfirmationTests
{
    [Fact]
    public async Task Placing_an_order_leaves_it_payment_pending_with_the_configured_deposit()
    {
        var (_, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        result.Value.Status.Should().Be(OrderStatus.PaymentPending.ToString());

        placed!.Status.Should().Be(OrderStatus.PaymentPending,
            "FR-CUST-012 / BR-PAY-007: placement no longer confirms the order");
        placed.DepositAmount.Should().Be(150_000m, "50% of the 300,000 quote total");
        placed.PaidAmount.Should().Be(0m);
        placed.IsDeposited.Should().BeFalse();
        placed.IsSettled.Should().BeFalse();
    }

    [Fact]
    public async Task Placing_an_order_does_not_record_a_payment_or_confirm_timestamp()
    {
        var (_, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        placed!.PaymentTransactionId.Should().BeNull();
        placed.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Placing_an_order_raises_no_OrderConfirmedEvent_until_the_deposit_is_paid()
    {
        var (_, _, _, result, placed) = await PlaceWithQuote();

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        placed!.DomainEvents.Should().NotContain(e => e is OrderConfirmedEvent,
            "jobs must only be scheduled once the deposit is actually paid");
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

        var configuration = Substitute.For<IConfiguration>();
        configuration["Payment:DepositPercent"].Returns("50");

        var handler = new PlaceOrderCommandHandler(quotes, orders, uow, clock, configuration);

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
