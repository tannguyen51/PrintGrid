using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public class OrderContractTests
{
    [Fact]
    public void Contract_number_is_empty_until_the_order_is_confirmed()
    {
        var order = OrderFromReadyQuote();

        order.ContractNumber.Should().BeNull();
    }

    [Fact]
    public void ConfirmPayment_issues_exactly_one_contract_number()
    {
        var order = OrderFromReadyQuote();

        order.ConfirmPayment("txn_123").IsSuccess.Should().BeTrue();

        order.ContractNumber.Should().NotBeNullOrWhiteSpace();
        order.ContractNumber.Should().MatchRegex("^PG-CT-\\d{8}-[0-9A-F]{12}$");
        order.ContractNumber.Should().StartWith($"PG-CT-{order.ConfirmedAt!.Value:yyyyMMdd}-");
        order.ContractNumber.Should().EndWith(order.Id.ToString("N")[..12].ToUpperInvariant());
    }

    [Fact]
    public void A_second_confirmation_does_not_change_the_contract_number()
    {
        var order = OrderFromReadyQuote();
        order.ConfirmPayment("txn_123");
        var issued = order.ContractNumber;

        var second = order.ConfirmPayment("txn_456");

        second.IsFailure.Should().BeTrue();
        order.ContractNumber.Should().Be(issued);
    }

    [Fact]
    public void Terms_acceptance_is_stored_with_the_moment_of_placement()
    {
        var at = new DateTime(2026, 11, 4, 9, 30, 0, DateTimeKind.Utc);

        var order = OrderFromReadyQuote(termsAccepted: true, termsAcceptedAtUtc: at);

        order.TermsAccepted.Should().BeTrue();
        order.TermsAcceptedAtUtc.Should().Be(at);
    }

    [Fact]
    public void Contract_numbers_differ_between_orders()
    {
        var a = OrderFromReadyQuote();
        var b = OrderFromReadyQuote();
        a.ConfirmPayment("txn_a");
        b.ConfirmPayment("txn_b");

        a.ContractNumber.Should().NotBe(b.ContractNumber);
    }

    private static Order OrderFromReadyQuote(bool termsAccepted = true, DateTime? termsAcceptedAtUtc = null)
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
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "v1", "test basis");

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        return Order.CreateFromQuote(
            quote, address, $"PG-{DateTime.UtcNow:yyyyMMdd}-00001", 50m,
            termsAccepted: termsAccepted, termsAcceptedAtUtc: termsAcceptedAtUtc);
    }
}
