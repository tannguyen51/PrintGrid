using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Analytics.Domain.Entities;

public sealed class LabPayable : Entity<Guid>
{
    private LabPayable() { }
    public Guid JobId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid LabId { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DeductionAmount { get; private set; }
    public decimal NetAmount => GrossAmount - DeductionAmount;
    public string Currency { get; private set; } = "VND";
    public string Status { get; private set; } = "Eligible";
    public DateTime EligibleAtUtc { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? PaidBy { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string? PaymentReference { get; private set; }

    public static LabPayable Create(Guid jobId, Guid orderId, Guid labId, decimal gross, decimal deduction,
        string currency, DateTime eligibleAtUtc) => new()
    {
        Id = Guid.NewGuid(), JobId = jobId, OrderId = orderId, LabId = labId,
        GrossAmount = gross, DeductionAmount = deduction, Currency = currency, EligibleAtUtc = eligibleAtUtc
    };

    public Result Approve(Guid actor, DateTime now)
    {
        if (Status != "Eligible") return Result.Failure(Error.Conflict("Khoản công nợ không còn ở trạng thái chờ duyệt."));
        Status = "Approved"; ApprovedBy = actor; ApprovedAtUtc = now; return Result.Success();
    }

    public bool NeedsDeductionReconciliation(decimal deduction)
    {
        if (deduction < 0m || deduction > GrossAmount) throw new ArgumentOutOfRangeException(nameof(deduction));
        return DeductionAmount != deduction && (Status != "Paid" || NetAmount == 0m && deduction < DeductionAmount);
    }

    /// <summary>Correct outstanding fees, including an erroneous zero-value paid marker. Require fresh approval.</summary>
    public bool ReconcileUnpaidDeduction(decimal deduction)
    {
        if (!NeedsDeductionReconciliation(deduction)) return false;
        DeductionAmount = deduction;
        Status = "Eligible";
        ApprovedBy = null;
        ApprovedAtUtc = null;
        PaidBy = null;
        PaidAtUtc = null;
        PaymentReference = null;
        return true;
    }

    public Result MarkPaid(Guid actor, string reference, DateTime now)
    {
        if (Status != "Approved") return Result.Failure(Error.Conflict("Khoản công nợ phải được duyệt trước khi chi."));
        if (string.IsNullOrWhiteSpace(reference)) return Result.Failure(Error.Validation("Mã tham chiếu chi tiền là bắt buộc."));
        Status = "Paid"; PaidBy = actor; PaidAtUtc = now; PaymentReference = reference.Trim(); return Result.Success();
    }
}
