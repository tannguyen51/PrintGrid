using NSubstitute;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class SlicedQuoteFlowTests
{
    [Fact]
    public async Task Quote_scales_size_slices_each_eligible_machine_and_freezes_cheapest_batch_into_order()
    {
        var customer = Guid.NewGuid();
        var models = Substitute.For<IModelRepository>();
        var model = Model.Create(customer, "part", null, "part.3mf", "3MF", 1000, null,
            "printgrid-models/test/part.3mf", "hash");
        model.ApplyGeometry(120, 80, 160, 500, 60, 10, true, true, true);
        model.Approve(null, DateTime.UtcNow);
        models.GetByIdForCustomerAsync(customer, model.Id, Arg.Any<CancellationToken>()).Returns(model);
        var quotes = Substitute.For<IQuoteRepository>();
        Quote? captured = null;
        quotes.AddAsync(Arg.Do<Quote>(q => captured = q), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc));
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var slicer = Substitute.For<IQuoteSlicer>();
        slicer.GetConfiguredMachineIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { first, second });
        slicer.SliceAsync(Arg.Any<QuoteSliceRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<QuoteSliceRequest>();
            return new QuoteSliceResult(new[] { new QuoteColorUsage("BLUE", 460), new QuoteColorUsage("WHITE", 460) },
                3900, "CuraEngine", request.MachineId == first ? 6000 : 5000, .08m);
        });
        var probe = Substitute.For<IProductionCapacityProbe>();
        probe.FindEarliestFeasibleDeliveryAsync(Arg.Any<IReadOnlyList<TrialPlacementLine>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => new TrialPlacementResult(new DateOnly(2026, 11, 10), Guid.NewGuid(),
                call.Arg<IReadOnlyList<TrialPlacementLine>>()[0].RequiredMachineId!.Value, "private", "HCM"));
        var rates = PricingParameterSet.Active with { PolishingPerUnit = 30000,
            ColorPricePerKg = new Dictionary<string, decimal> { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000 } };
        var policy = Substitute.For<ICommercialPolicyProvider>();
        policy.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new CommercialPolicy(rates, 10_000_000, 48));
        var handler = new CreateQuoteCommandHandler(models, quotes, Substitute.For<IUnitOfWork>(), clock, probe, policy, slicer);
        var result = await handler.Handle(new(customer, model.Id, "PLA", "BLUE", .2m, 30, 5, .2m,
            "M", new[] { "BLUE", "WHITE" }, true), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().Calculation!.WidthMm.Should().Be(150);
        result.Value.Items.Single().Calculation!.DepthMm.Should().Be(100);
        result.Value.Items.Single().Calculation!.HeightMm.Should().Be(200);
        result.Value.PlacementBasis.Should().BeNull();
        captured!.Items.Single().QuotedMachineId.Should().Be(second);
        await slicer.Received(2).SliceAsync(Arg.Is<QuoteSliceRequest>(r => r.Scale == 1.25m && r.Quantity == 5 && r.Colors.Count == 2), Arg.Any<CancellationToken>());
        var order = Order.CreateFromQuote(captured, Address.Create("1 A", "B", "C", "HCM", "70000"), "PG-TEST");
        order.TotalPrice.Should().Be(captured.TotalPrice);
        order.Items.Single().CalculationJson.Should().Be(captured.Items.Single().CalculationJson);
        order.Items.Single().QuotedMachineId.Should().Be(second);
        order.Items.Single().LineTotal.Amount.Should().Be(result.Value.TotalPrice);
    }

    [Fact]
    public void Unpriced_color_falls_back_to_material_rate_without_adding_a_color_surcharge()
    {
        var rates = PricingParameterSet.Active with { ColorPricePerKg = new Dictionary<string, decimal>() };
        var result = SlicedQuotePricing.Calculate(rates, "PLA", "ORIGINAL", 10, 10, 10, 1, 1,
            new(new[] { new QuoteColorUsage("BLUE", 10) }, 60, "CuraEngine"), false);
        result.MaterialAmount.Should().Be(10 * rates.PricePerGram["PLA"]);
    }

    [Fact]
    public void Real_cli_summary_is_used_instead_of_placeholder_gcode_header()
    {
        var result = PrintGrid.Modules.Customer.Infrastructure.Slicing.CuraQuoteSlicer.ParseEngineSummary(
            "[info] Print time (s): 3600\n[info] Filament (mm^3): 1000\n[info] Filament 2: 2000\n[info] Filament 3: 3000",
            new[] { "BLUE", "WHITE", "SIGNALRED" }, 1.24m, 5);
        result!.PrintMinutes.Should().Be(300);
        result.Colors.Select(c => c.Grams).Should().Equal(6.2m, 12.4m, 18.6m);
    }
}
