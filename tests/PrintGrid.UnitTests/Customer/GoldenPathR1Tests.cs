using FluentAssertions;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

/// <summary>
/// Sprint "golden path real data" acceptance tests (FR-SCHED-010, FR-SCHED-005,
/// FR-CUST-007, FR-SCHED-006 input chain) — week 1–2 gate G-A backend half.
/// </summary>
public class GoldenPathR1Tests
{
    // ── FR-SCHED-010 / BR-QUOTE-004: frozen version reproduces the old price ──────
    [Fact]
    public void Frozen_parameter_version_always_reproduces_its_price()
    {
        var legacy = QuotePricing.PriceLine(PricingParameterSet.V2026_09, 20m, 60, 20m, "PLA", 0.2m);
        var modern = QuotePricing.PriceLine(PricingParameterSet.V2026_10, 20m, 60, 20m, "PLA", 0.2m);

        modern.UnitPrice.Should().BeGreaterThan(legacy.UnitPrice,
            "a newer parameter set must cost more (rates were raised)");

        var rerun = QuotePricing.PriceLine(PricingParameterSet.ByVersion("2026.09"), 20m, 60, 20m, "PLA", 0.2m);
        rerun.UnitPrice.Should().Be(legacy.UnitPrice,
            "re-running with the frozen version returns the identical price");

        Math.Abs((rerun.MaterialCostAmount + rerun.MachineTimeCostAmount) - rerun.UnitPrice)
            .Should().BeLessThanOrEqualTo(2m, "itemized split must sum to the unit price (BR-QUOTE-005)");
    }

    // ── FR-CUST-007: no terms, no order ───────────────────────────────────────────
    [Fact]
    public void Placing_an_order_without_accepting_terms_is_invalid()
    {
        var validator = new PlaceOrderCommandValidator();

        var withoutTerms = new PlaceOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), "1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000", false);
        var withTerms = withoutTerms with { AcceptTerms = true };

        validator.Validate(withoutTerms).IsValid.Should().BeFalse();
        validator.Validate(withTerms).IsValid.Should().BeTrue();
    }

    // ── FR-SCHED-005: promised date comes from the trial placement, not +3 days ───
    [Fact]
    public async Task Quote_date_and_version_come_from_trial_placement_and_active_parameter_set()
    {
        var customerId = Guid.NewGuid();
        var quotes = Substitute.For<IQuoteRepository>();
        Quote? captured = null;
        quotes.AddAsync(Arg.Do<Quote>(q => captured = q), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var handler = BuildHandler(quotes, customerId, feasible: true);

        var result = await handler.Handle(
            new CreateQuoteCommand(customerId, Guid.NewGuid(), "PLA", "BLACK", 0.2m, 20, 2, 0.3m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var promised = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9));
        result.Value.PromisedDeliveryDate.Should().Be(promised, "the probe's trial date must win over any constant");
        result.Value.PricingVersion.Should().Be(PricingParameterSet.Active.Version);
        result.Value.PlacementBasis.Should().NotBeNullOrWhiteSpace();
        var item = result.Value.Items.Single();
        item.MaterialCostAmount.Should().BeGreaterThan(0);
        item.MachineTimeCostAmount.Should().BeGreaterThan(0);
        captured.Should().NotBeNull();
    }

    [Fact]
    public async Task Quote_is_refused_when_no_machine_in_the_network_can_produce_it()
    {
        var customerId = Guid.NewGuid();
        var quotes = Substitute.For<IQuoteRepository>();
        Quote? captured = null;
        quotes.AddAsync(Arg.Do<Quote>(q => captured = q), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var handler = BuildHandler(quotes, customerId, feasible: false);

        var result = await handler.Handle(
            new CreateQuoteCommand(customerId, Guid.NewGuid(), "PLA", "BLACK", 0.2m, 20, 2, 0.3m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        captured.Should().NotBeNull("a failed quote must still be recorded for audit");
        captured!.Status.Should().Be(QuoteStatus.Failed);
    }

    // ── FR-SCHED-006 input chain: real estimates ride OrderConfirmedEvent ─────────
    [Fact]
    public void Confirmed_order_carries_frozen_estimates_into_the_event()
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(
            Guid.NewGuid(), 3,
            PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.3m),
            Money.Of(150_000m), 180, 40m, 90_000m, 60_000m, 60m, 60m, 40m);
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9)), PricingParameterSet.Active.Version);

        var order = Order.CreateFromQuote(
            quote,
            Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000"),
            "PG-TEST-00001");

        order.ConfirmPayment("txn-1");

        var evt = order.DomainEvents.OfType<OrderConfirmedEvent>().Single();
        var item = evt.Items.Single();
        item.EstimatedPrintMinutes.Should().Be(180);
        item.EstimatedMaterialGrams.Should().Be(40m);
        item.BoundingWidthMm.Should().Be(60m);
    }

    private static CreateQuoteCommandHandler BuildHandler(
        IQuoteRepository quotes, Guid customerId, bool feasible)
    {
        var models = Substitute.For<IModelRepository>();
        var model = Model.Create(customerId, "bracket", null, "bracket.stl", "STL", 1_048_576, null);
        model.ApplyGeometry(60m, 60m, 40m, 20m, 60);
        models.GetByIdForCustomerAsync(customerId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Model?>(model));

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        clock.Today.Returns(DateOnly.FromDateTime(DateTime.UtcNow));

        var probe = Substitute.For<IProductionCapacityProbe>();
        var promised = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9));
        probe.FindEarliestFeasibleDeliveryAsync(
                Arg.Any<IReadOnlyList<TrialPlacementLine>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(feasible
                ? Task.FromResult<TrialPlacementResult?>(
                    new TrialPlacementResult(promised, Guid.NewGuid(), Guid.NewGuid(), "lab A · machine M1 (trial)"))
                : Task.FromResult<TrialPlacementResult?>(null));

        return new CreateQuoteCommandHandler(models, quotes, uow, clock, probe);
    }
}
