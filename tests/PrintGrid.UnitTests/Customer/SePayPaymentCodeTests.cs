using PrintGrid.Modules.Customer.Application.Commands.Payments;

namespace PrintGrid.UnitTests.Customer;

public sealed class SePayPaymentCodeTests
{
    [Fact]
    public void Payment_code_round_trips_the_order_id()
    {
        var orderId = Guid.NewGuid();
        var code = SePayPaymentCode.ForOrder(orderId);

        SePayPaymentCode.TryGetOrderId(code, out var parsed).Should().BeTrue();
        parsed.Should().Be(orderId);
        code.Should().MatchRegex("^PG[0-9A-HJKMNP-TV-Z]{26}$");
    }

    [Fact]
    public void Payment_code_is_found_inside_a_bank_description()
    {
        var orderId = Guid.NewGuid();
        var description = $"chuyen tien {SePayPaymentCode.ForOrder(orderId)} thanh toan";

        SePayPaymentCode.TryGetOrderId(description, out var parsed).Should().BeTrue();
        parsed.Should().Be(orderId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PG123")]
    [InlineData("NO_PAYMENT_CODE")]
    public void Invalid_payment_code_is_rejected(string? value)
    {
        SePayPaymentCode.TryGetOrderId(value, out _).Should().BeFalse();
    }
}
