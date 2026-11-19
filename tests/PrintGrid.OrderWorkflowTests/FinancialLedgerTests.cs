using PrintGrid.Modules.Analytics.Domain.Entities;

namespace PrintGrid.OrderWorkflowTests;

public sealed class FinancialLedgerTests
{
    [Fact]
    public void Receipt_keeps_reconciliation_evidence()
    {
        var actor = Guid.NewGuid(); var order = Guid.NewGuid(); var at = DateTime.UtcNow;
        var row = FinancialTransaction.Create(order, "Receipt", 125_000m, "VND", "BANK-001", "hook-001", actor, at);
        row.OrderId.Should().Be(order); row.Amount.Should().Be(125_000m);
        row.Reference.Should().Be("BANK-001"); row.IdempotencyKey.Should().Be("hook-001");
        row.ConfirmedBy.Should().Be(actor); row.ConfirmedAtUtc.Should().Be(at);
    }

    [Fact]
    public void Lab_payable_requires_approval_before_payment_and_preserves_snapshot()
    {
        var payable = LabPayable.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100_000m, 20_000m, "VND", DateTime.UtcNow);
        payable.NetAmount.Should().Be(80_000m);
        payable.MarkPaid(Guid.NewGuid(), "BANK-FAIL", DateTime.UtcNow).IsFailure.Should().BeTrue();
        payable.Approve(Guid.NewGuid(), DateTime.UtcNow).IsSuccess.Should().BeTrue();
        payable.MarkPaid(Guid.NewGuid(), "BANK-OK", DateTime.UtcNow).IsSuccess.Should().BeTrue();
        payable.Status.Should().Be("Paid"); payable.PaymentReference.Should().Be("BANK-OK");
    }
}
