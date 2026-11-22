using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class QuoteReviewTests
{
    [Fact]
    public void Engine_draft_has_no_expiry_until_approval()
    {
        var quote = Draft(500_000m);

        quote.Status.Should().Be(QuoteStatus.Draft);
        quote.ExpiresAt.Should().BeNull();

        var approvedAt = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc);
        quote.Approve(Guid.NewGuid(), 500_000m, quote.PromisedDeliveryDate, null,
            approvedAt, TimeSpan.FromHours(48), false, 10m).IsSuccess.Should().BeTrue();

        quote.ExpiresAt.Should().Be(approvedAt.AddHours(48));
        quote.ApprovedAt.Should().Be(approvedAt);
    }

    [Fact]
    public void Changed_draft_requires_reason_and_must_stay_inside_band()
    {
        var missingReason = Draft(500_000m);
        missingReason.Approve(Guid.NewGuid(), 525_000m, missingReason.PromisedDeliveryDate, null,
            DateTime.UtcNow, TimeSpan.FromHours(48), false, 10m).IsFailure.Should().BeTrue();

        var outsideBand = Draft(500_000m);
        outsideBand.Approve(Guid.NewGuid(), 560_000m, outsideBand.PromisedDeliveryDate, "Phức tạp hơn dự kiến",
            DateTime.UtcNow, TimeSpan.FromHours(48), false, 10m).IsFailure.Should().BeTrue();

        var valid = Draft(500_000m);
        valid.Approve(Guid.NewGuid(), 525_000m, valid.PromisedDeliveryDate, "Cần thêm support",
            DateTime.UtcNow, TimeSpan.FromHours(48), false, 10m).IsSuccess.Should().BeTrue();
        valid.AdjustmentReason.Should().Be("Cần thêm support");
    }

    [Fact]
    public void Order_created_from_draft_waits_for_ops_then_moves_to_payment()
    {
        var quote = Draft(100_000m, quantity: 5); // 500,000 VND for the whole order
        var order = Order.CreateFromQuote(quote,
            Address.Create("123 Quang Trung", "Long Thạnh Mỹ", "Quận 9", "TP.HCM", "70000"),
            "PG-TEST-00001", depositPercent: 50m);

        order.Status.Should().Be(OrderStatus.AwaitingApproval);
        quote.SubmitForReview().IsSuccess.Should().BeTrue();

        quote.Approve(Guid.NewGuid(), 525_000m, quote.PromisedDeliveryDate, "Điều chỉnh sau thẩm định",
            DateTime.UtcNow, TimeSpan.FromHours(48), false, 10m).IsSuccess.Should().BeTrue();
        order.ApplyApprovedQuote(quote).IsSuccess.Should().BeTrue();

        order.Status.Should().Be(OrderStatus.PaymentPending);
        order.TotalPrice.Amount.Should().Be(525_000m);
        order.DepositAmount.Should().Be(262_500m);
    }

    private static Quote Draft(decimal price, int quantity = 1)
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(48));
        quote.AddItem(Guid.NewGuid(), quantity,
            PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.2m),
            Money.Of(price), 60, 20m, price / 2, price / 2, 10m, 10m, 10m);
        quote.MarkDraft(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), PricingParameterSet.Active.Version);
        return quote;
    }
}
