using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public sealed class PinnedQuoteFlowTests
{
    private sealed class Setup
    {
        public readonly Guid Customer = Guid.NewGuid(), ReferenceMachine = Guid.NewGuid(), PinnedMachine = Guid.NewGuid(), ModelId = MachineModelIdentity.For("Maker", "X");
        public readonly Guid ReferenceProfile = Guid.NewGuid(), PinnedProfile = Guid.NewGuid();
        public readonly IQuoteRepository Quotes = Substitute.For<IQuoteRepository>();
        public readonly IMachineFilterService Machines = Substitute.For<IMachineFilterService>();
        public readonly IQuoteSlicer Slicer = Substitute.For<IQuoteSlicer>();
        public readonly IProductionCapacityProbe Capacity = Substitute.For<IProductionCapacityProbe>();
        public readonly IDateTimeProvider Clock = Substitute.For<IDateTimeProvider>();
        public readonly Model Product;
        public readonly CreateQuoteCommand Command;
        public readonly CreateQuoteCommandHandler Handler;
        public Quote? Saved;

        public Setup(int pinnedHours = 58, bool unavailable = false)
        {
            Clock.UtcNow.Returns(new DateTime(2026, 11, 22, 8, 0, 0, DateTimeKind.Utc));
            Clock.Today.Returns(new DateOnly(2026, 11, 22));
            Product = Model.Create(Customer, "Part", null, "part.3mf", "3MF", 1000, null, "models/part.3mf", "hash");
            Product.ApplyGeometry(120, 80, 160, 500, 60, 10, true, true, true); Product.Approve(null, Clock.UtcNow);
            var models = Substitute.For<IModelRepository>();
            models.GetByIdForCustomerAsync(Customer, Product.Id, Arg.Any<CancellationToken>()).Returns(Product);
            Quotes.AddAsync(Arg.Do<Quote>(q => Saved = q), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            Slicer.GetConfiguredMachineIdsAsync(Arg.Any<CancellationToken>()).Returns([ReferenceMachine, PinnedMachine]);
            Machines.FilterCandidatesAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<MachineSelectionCriteria>(), Arg.Any<CancellationToken>())
                .Returns([ReferenceMachine, PinnedMachine]);
            Machines.GetOptionsAsync(Arg.Any<MachineSelectionCriteria>(), 3, 2, 1.15m, Arg.Any<CancellationToken>())
                .Returns(new MachineModelOptions([new(ModelId, "Maker · X", 3)], 4, 1.15m));
            Machines.FilterByPinModelAsync(ModelId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(((IReadOnlyList<Guid>)[PinnedMachine], (IReadOnlyList<Guid>)[ReferenceMachine, PinnedMachine]));
            Machines.GetReferenceMachineAsync(Arg.Any<IReadOnlyList<Guid>>(), "PLA", null, Arg.Any<CancellationToken>())
                .Returns(call => call.Arg<IReadOnlyList<Guid>>().Contains(ReferenceMachine)
                    ? new QuoteProfileSource(ReferenceMachine, ReferenceProfile) : new QuoteProfileSource(PinnedMachine, PinnedProfile));
            Machines.GetCalibrationFactorAsync(null, Arg.Any<CancellationToken>()).Returns(1.10m);
            Machines.GetCalibrationFactorAsync(ModelId, Arg.Any<CancellationToken>()).Returns(1.05m);
            Slicer.SliceAsync(Arg.Any<QuoteSliceRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var pinned = call.Arg<QuoteSliceRequest>().MachineId == PinnedMachine;
                var grams = pinned ? 540 : 560;
                return new QuoteSliceResult([new("BLUE", grams), new("WHITE", grams), new("SIGNALRED", grams)],
                    (pinned ? pinnedHours : 65) * 60, PlateCount: 2, ProfileId: pinned ? PinnedProfile : ReferenceProfile, ProfileVersion: "profile-v1");
            });
            Capacity.FindEarliestFeasibleDeliveryAsync(Arg.Any<IReadOnlyList<TrialPlacementLine>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(call => call.Arg<IReadOnlyList<TrialPlacementLine>>()[0].Requirements?.PinnedModelId.HasValue == true
                    ? unavailable ? null : new TrialPlacementResult(new DateOnly(2026, 12, 5), "secret", "Theo lịch mạng lưới")
                    : new TrialPlacementResult(new DateOnly(2026, 12, 1), "secret", "Theo lịch mạng lưới"));
            var rates = PricingParameterSet.Active with { PricePerPrintHour = 6000, FailureRate = .08m, PolishingPerUnit = 30000,
                ColorPricePerKg = new Dictionary<string, decimal> { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000, ["PLA:SIGNALRED"] = 310000 } };
            var policy = Substitute.For<ICommercialPolicyProvider>();
            policy.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new CommercialPolicy(rates, 0, 48, new LabScoreParameters()));
            Handler = new(models, Quotes, Substitute.For<IUnitOfWork>(), Clock, Capacity, Machines, policy, Slicer);
            Command = new(Customer, Product.Id, "PLA", "BLUE", .2m, 30, 5, .2m, "ORIGINAL", ["BLUE", "WHITE", "SIGNALRED"], true,
                MaxColors: 3, BindingType: 1, PinnedModelId: ModelId);
        }
    }

    [Theory]
    [InlineData(58)]
    [InlineData(200)]
    public async Task Customer_pinned_choice_is_retained_even_when_it_costs_more(int pinnedHours)
    {
        var s = new Setup(pinnedHours);
        var result = await s.Handler.Handle(s.Command, default);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsPinnedModelQuote.Should().BeTrue();
        result.Value.UnpinnedPriceVnd.Should().Be(1231000);
        result.Value.PinSurchargeCoefficient.Should().Be(1.15m);
        if (pinnedHours == 58) result.Value.TotalPrice.Should().Be(1317000);
        else result.Value.TotalPrice.Should().BeGreaterThan(result.Value.UnpinnedPriceVnd!.Value * 1.1m);
        var item = s.Saved!.Items.Single();
        item.QuotedMachineId.Should().Be(s.PinnedMachine);
        item.ProfileId.Should().Be(s.PinnedProfile);
        item.ProfileVersion.Should().Be("profile-v1");
        item.BatchTotalAmount.Should().Be(result.Value.TotalPrice);
        var calculation = JsonSerializer.Deserialize<QuotePriceBreakdown>(item.CalculationJson!)!;
        calculation.Requirements!.PinnedModelId.Should().Be(s.ModelId);
        calculation.PricingParameters!.Version.Should().Be(s.Saved.PricingVersion);

        // Browser-supplied context cannot replace the frozen choice, even when an old API client sends it.
        var order = Order.CreateFromQuote(s.Saved, Address.Create("1 A", "B", "C", "HCM", ""), "TEST",
            modelBindingKind: "không", pinnedModelId: Guid.NewGuid(), techConstraintsJson: "{}");
        order.Items.Single().PinnedModelId.Should().Be(s.ModelId);
        order.Items.Single().TechConstraintsJson.Should().Contain(s.ModelId.ToString());
    }

    [Fact]
    public async Task Unavailable_pinned_model_fails_instead_of_selling_unpinned_option()
    {
        var s = new Setup(unavailable: true);
        var result = await s.Handler.Handle(s.Command, default);
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("không tự đổi model");
        s.Saved.Should().BeNull();
    }

    [Fact]
    public async Task Forged_or_ineligible_model_is_rejected_before_slicing()
    {
        var s = new Setup();
        var result = await s.Handler.Handle(s.Command with { PinnedModelId = s.ReferenceMachine }, default);
        result.IsFailure.Should().BeTrue();
        await s.Slicer.DidNotReceive().SliceAsync(Arg.Any<QuoteSliceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delivery_preview_rechecks_same_model_without_locking_to_the_original_machine()
    {
        var s = new Setup();
        (await s.Handler.Handle(s.Command, default)).IsSuccess.Should().BeTrue();
        s.Capacity.ClearReceivedCalls();
        var delivery = new OrderDeliveryService(s.Capacity, s.Clock, new ConfigurationBuilder().Build());
        (await delivery.AssessAsync(s.Saved!, new DateOnly(2026, 12, 10), default)).IsSuccess.Should().BeTrue();
        await s.Capacity.Received().FindEarliestFeasibleDeliveryAsync(Arg.Is<IReadOnlyList<TrialPlacementLine>>(lines =>
            lines.All(l => l.RequiredMachineId == null && l.Requirements!.PinnedModelId == s.ModelId && l.FrozenDuration)),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
