using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

/// <summary>
/// FR-CUST-012 (deposit/settlement), FR-ANAL-005 (refund decisions) and FR-CUST-013 (design intake)
/// domain rules.
/// </summary>
public class CommerceFlowTests
{
    // ----- FR-CUST-012 / BR-PAY-007/008 ------------------------------------------------

    [Fact]
    public void Payment_below_the_deposit_keeps_the_order_payment_pending_and_raises_no_event()
    {
        var order = OrderWithDeposit();

        var result = order.ApplyPayment(100_000m, "txn-partial");

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        order.Status.Should().Be(OrderStatus.PaymentPending);
        order.PaidAmount.Should().Be(100_000m);
        order.DepositAmount.Should().Be(150_000m);
        order.IsDeposited.Should().BeFalse();
        order.IsSettled.Should().BeFalse();
        order.PaymentTransactionId.Should().Be("txn-partial");
        order.DomainEvents.Should().NotContain(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void Reaching_the_deposit_confirms_the_order_once_and_raises_the_job_event()
    {
        var order = OrderWithDeposit();

        order.ApplyPayment(100_000m, "txn-1");
        var result = order.ApplyPayment(50_000m, "txn-2");

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.IsDeposited.Should().BeTrue();
        order.IsSettled.Should().BeFalse();
        order.ConfirmedAt.Should().NotBeNull();
        order.PaymentTransactionId.Should().Be("txn-2");
        order.DomainEvents.OfType<OrderConfirmedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Settling_the_remaining_balance_marks_the_order_settled_without_a_second_confirmation()
    {
        var order = OrderWithDeposit();
        order.ApplyPayment(150_000m, "deposit");

        var result = order.ApplyPayment(150_000m, "settlement");

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        order.PaidAmount.Should().Be(300_000m);
        order.IsSettled.Should().BeTrue();
        order.DomainEvents.OfType<OrderConfirmedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Payment_cannot_exceed_the_remaining_balance()
    {
        var order = OrderWithDeposit();

        var result = order.ApplyPayment(300_001m, "too-much");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        order.PaidAmount.Should().Be(0m);
        order.Status.Should().Be(OrderStatus.PaymentPending);
    }

    [Fact]
    public void Payment_must_be_positive()
    {
        var order = OrderWithDeposit();

        var result = order.ApplyPayment(0m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    // ----- FR-ANAL-005 refund state machine -------------------------------------------

    [Fact]
    public void Pending_refund_can_be_approved_only_once()
    {
        var refund = QueuedRefund();

        var approved = refund.Approve();
        var again = refund.Approve();

        approved.IsSuccess.Should().BeTrue();
        refund.Status.Should().Be("Approved");
        again.IsFailure.Should().BeTrue();
        again.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void Pending_refund_can_be_rejected()
    {
        var refund = QueuedRefund();

        var result = refund.Reject();

        result.IsSuccess.Should().BeTrue();
        refund.Status.Should().Be("Rejected");
    }

    // ----- FR-CUST-013 / BR-IP-004 design intake --------------------------------------

    [Fact]
    public void Design_request_starts_pending_and_keeps_the_reference_image_keys()
    {
        var customerId = Guid.NewGuid();

        var request = DesignRequest.Create(
            customerId,
            "  Bracket for a robot arm  ",
            "  Needs M3 mounting holes  ",
            new[] { "design/a.png", " ", "design/b.png" });

        request.CustomerId.Should().Be(customerId);
        request.Title.Should().Be("Bracket for a robot arm");
        request.Description.Should().Be("Needs M3 mounting holes");
        request.Status.Should().Be("Pending");
        request.QuoteId.Should().BeNull();
        request.ReferenceImageKeys.Should().Equal("design/a.png", "design/b.png");
        request.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Design_request_status_only_accepts_the_documented_lifecycle()
    {
        var request = DesignRequest.Create(Guid.NewGuid(), "Widget", "Description");

        request.ChangeStatus("inreview").IsSuccess.Should().BeTrue();
        request.Status.Should().Be("InReview");

        var invalid = request.ChangeStatus("ShippedToMars");

        invalid.IsFailure.Should().BeTrue();
        invalid.Error.Code.Should().Be("validation_error");
        request.Status.Should().Be("InReview");
    }

    // ----- helpers --------------------------------------------------------------------

    private static Order OrderWithDeposit()
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
        quote.MarkReady(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), PricingParameterSet.Active.Version, "test basis");

        var address = Address.Create("1 Dai Co Viet", "Bach Khoa", "Hai Ba Trung", "Ha Noi", "100000");
        return Order.CreateFromQuote(quote, address, "PG-20260902-00001", depositPercent: 50m);
    }

    private static RefundRequest QueuedRefund() => RefundRequest.Queue(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        100_000m, "VND", "txn-refund", "Customer rejected the reschedule", DateTime.UtcNow);
}
