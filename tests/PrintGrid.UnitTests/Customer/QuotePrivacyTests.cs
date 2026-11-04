using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class QuotePrivacyTests
{
    private const string InternalBasis =
        "In xong ước tính 05/11 10:00 UTC tại Xưởng In Bí Mật · máy Prusa MK4, cộng 1 ngày vận chuyển về hub";
    private const string CustomerSummary =
        "In tại Quận Tân Bình, TP. Hồ Chí Minh — hoàn tất dự kiến 05/11 10:00 UTC; cộng 1 ngày vận chuyển về hub";

    [Fact]
    public void The_customer_projection_hides_the_internal_placement_basis()
    {
        var quote = DraftQuoteWithPlacement();

        var dto = QuoteMappings.ToCustomerDto(quote);

        dto.PlacementBasis.Should().BeNull("the lab and machine identity must never reach the customer");
        dto.CustomerPlacementSummary.Should().Be(CustomerSummary);
    }

    [Fact]
    public void The_customer_summary_never_contains_lab_or_machine_names()
    {
        var dto = QuoteMappings.ToCustomerDto(DraftQuoteWithPlacement());

        dto.CustomerPlacementSummary.Should().NotContain("Xưởng");
        dto.CustomerPlacementSummary.Should().NotContain("Prusa");
        dto.CustomerPlacementSummary.Should().Contain("Quận Tân Bình");
    }

    [Fact]
    public void Staff_still_see_the_full_internal_basis()
    {
        var dto = QuoteMappings.ToDto(DraftQuoteWithPlacement());

        dto.PlacementBasis.Should().Be(InternalBasis);
        dto.CustomerPlacementSummary.Should().Be(CustomerSummary);
    }

    private static Quote DraftQuoteWithPlacement()
    {
        var quote = Quote.CreatePending(Guid.NewGuid(), TimeSpan.FromHours(24));
        quote.AddItem(
            Guid.NewGuid(),
            quantity: 1,
            PrintConfiguration.Create("PLA", "BLACK", 0.2m, 20, 0.3m),
            Money.Of(150_000m),
            estimatedPrintMinutes: 180,
            estimatedMaterialGrams: 40m,
            materialCostAmount: 90_000m,
            machineTimeCostAmount: 60_000m,
            boundingWidthMm: 60m,
            boundingDepthMm: 60m,
            boundingHeightMm: 40m);

        quote.MarkDraft(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            "v1",
            InternalBasis,
            CustomerSummary);

        return quote;
    }
}
