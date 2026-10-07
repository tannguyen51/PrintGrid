using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Quote : AggregateRoot<Guid>
{
    private readonly List<QuoteItem> _items = new();

    public Guid CustomerId { get; private set; }
    public QuoteStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public DateOnly PromisedDeliveryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? FailureReason { get; private set; }

    /// <summary>Pricing parameter-set version frozen at quote time (BR-QUOTE-004 / FR-SCHED-010).</summary>
    public string PricingVersion { get; private set; } = string.Empty;

    /// <summary>Human-readable basis of the promised date — which lab/machine timeline the trial used (FR-SCHED-005).</summary>
    public string? PlacementBasis { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public bool AutoApproved { get; private set; }
    public decimal? EngineTotalAmount { get; private set; }
    public DateOnly? EnginePromisedDeliveryDate { get; private set; }
    public string? AdjustmentReason { get; private set; }

    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();

    public bool IsExpired(DateTime asOfUtc) => ExpiresAt.HasValue && asOfUtc >= ExpiresAt.Value;

    private Quote() { }

    public static Quote CreatePending(Guid customerId, TimeSpan validity)
    {
        var now = DateTime.UtcNow;
        return new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = QuoteStatus.Pending,
            CreatedAt = now
        };
    }

    public QuoteItem AddItem(
        Guid modelId,
        int quantity,
        PrintConfiguration configuration,
        Money unitPrice,
        int estimatedPrintMinutes,
        decimal estimatedMaterialGrams,
        decimal materialCostAmount,
        decimal machineTimeCostAmount,
        decimal? boundingWidthMm,
        decimal? boundingDepthMm,
        decimal? boundingHeightMm)
    {
        var item = QuoteItem.Create(
            Id, modelId, quantity, configuration, unitPrice, estimatedPrintMinutes, estimatedMaterialGrams,
            materialCostAmount, machineTimeCostAmount, boundingWidthMm, boundingDepthMm, boundingHeightMm);
        _items.Add(item);
        return item;
    }

    public Result MarkDraft(DateOnly promisedDeliveryDate, string pricingVersion, string? placementBasis = null)
    {
        if (Status != QuoteStatus.Pending)
            return Result.Failure(Error.Conflict("Only a pending quote can be marked ready"));
        if (_items.Count == 0)
            return Result.Failure(Error.Validation("A quote needs at least one item"));

        Status = QuoteStatus.Draft;
        PromisedDeliveryDate = promisedDeliveryDate;
        PricingVersion = pricingVersion;
        PlacementBasis = placementBasis;
        TotalPrice = _items.Aggregate(
            Money.Zero(_items[0].UnitPrice.Currency),
            (sum, item) => sum.Add(item.UnitPrice.Multiply(item.Quantity)));
        EngineTotalAmount = TotalPrice.Amount;
        EnginePromisedDeliveryDate = promisedDeliveryDate;
        AddDomainEvent(new QuoteDraftedEvent(Id, TotalPrice.Amount, promisedDeliveryDate, placementBasis));

        return Result.Success();
    }

    // Compatibility helper for existing callers/tests: publishing starts the validity clock now.
    public Result MarkReady(DateOnly promisedDeliveryDate, string pricingVersion, string? placementBasis = null)
    {
        var drafted = MarkDraft(promisedDeliveryDate, pricingVersion, placementBasis);
        return drafted.IsFailure ? drafted : Approve(null, TotalPrice.Amount, promisedDeliveryDate, null, DateTime.UtcNow, TimeSpan.FromHours(48), true, 100m);
    }

    public Result Approve(Guid? staffId, decimal totalAmount, DateOnly promisedDeliveryDate,
        string? reason, DateTime approvedAtUtc, TimeSpan validity, bool autoApproved, decimal adjustmentBandPercent)
    {
        if (Status != QuoteStatus.Draft)
            return Result.Failure(Error.Conflict("Only an engine draft can be approved"));
        if (totalAmount <= 0)
            return Result.Failure(Error.Validation("Approved total must be greater than zero"));

        var engineAmount = EngineTotalAmount ?? TotalPrice.Amount;
        var deltaPercent = engineAmount == 0 ? 0 : Math.Abs(totalAmount - engineAmount) / engineAmount * 100m;
        if (deltaPercent > adjustmentBandPercent)
            return Result.Failure(Error.Validation($"Price adjustment exceeds the configured {adjustmentBandPercent:0.##}% band"));

        var changed = totalAmount != engineAmount || promisedDeliveryDate != EnginePromisedDeliveryDate;
        if (changed && string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("A reason is required when changing the engine draft"));

        TotalPrice = Money.Of(totalAmount, TotalPrice.Currency);
        PromisedDeliveryDate = promisedDeliveryDate;
        AdjustmentReason = changed ? reason!.Trim() : null;
        ReviewedBy = staffId;
        AutoApproved = autoApproved;
        ApprovedAt = approvedAtUtc;
        ExpiresAt = approvedAtUtc.Add(validity);
        Status = QuoteStatus.Ready;
        AddDomainEvent(new QuoteApprovedEvent(
            Id, staffId, engineAmount, totalAmount,
            EnginePromisedDeliveryDate ?? promisedDeliveryDate, promisedDeliveryDate,
            AdjustmentReason, autoApproved, approvedAtUtc));
        return Result.Success();
    }

    public void MarkFailed(string reason)
    {
        Status = QuoteStatus.Failed;
        FailureReason = reason;
    }

    public Result MarkConverted(DateTime asOfUtc)
    {
        if (Status != QuoteStatus.Ready)
            return Result.Failure(Error.Conflict("Only a ready quote can be converted to an order"));
        if (IsExpired(asOfUtc))
        {
            Status = QuoteStatus.Expired;
            return Result.Failure(Error.Conflict("Quote has expired and must be regenerated"));
        }

        Status = QuoteStatus.Converted;
        return Result.Success();
    }
}
