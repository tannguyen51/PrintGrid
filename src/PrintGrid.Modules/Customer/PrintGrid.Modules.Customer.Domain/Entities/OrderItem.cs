using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ModelId { get; private set; }
    public int Quantity { get; private init; }
    public PrintConfiguration Configuration { get; private set; } = null!;
    public Money UnitPrice { get; private init; } = Money.Zero();
    public string ItemStatus { get; private set; } = "Pending";
    public string? CalculationJson { get; private set; }
    public decimal? BatchTotalAmount { get; private init; }
    public Guid? QuotedMachineId { get; private set; }

    /// <summary>Production estimates copied from the frozen quote — feeds real job decomposition (FR-SCHED-006).</summary>
    public int EstimatedPrintMinutes { get; private set; }
    public decimal EstimatedMaterialGrams { get; private set; }
    public decimal? BoundingWidthMm { get; private set; }
    public decimal? BoundingDepthMm { get; private set; }
    public decimal? BoundingHeightMm { get; private set; }

    /// <summary>Hard technical constraints from Section 3 (doc §6.1). Stored as JSONB.</summary>
    public string? TechConstraintsJson { get; private set; }

    /// <summary>Machine-binding mode from doc Section 4/5 (doc §6.2): "không" | "theo_model" | "như_đơn_cũ".</summary>
    public string ModelBindingKind { get; private set; } = "không";

    /// <summary>Pinned machine model GUID when binding is "theo_model".</summary>
    public Guid? PinnedModelId { get; private set; }

    /// <summary>Binding level: "bắt_buộc" or "ưu_tiên" (default "bắt_buộc").</summary>
    public string BindingLevel { get; private set; } = "bắt_buộc";

    /// <summary>Reference order line ID when binding is "như_đơn_cũ".</summary>
    public Guid? ReferenceOrderLineId { get; private set; }

    public Money LineTotal => BatchTotalAmount.HasValue ? Money.Of(BatchTotalAmount.Value, UnitPrice.Currency) : UnitPrice.Multiply(Quantity);

    public void UpdateStatus(string newStatus) => ItemStatus = newStatus;

    /// <summary>Freeze tech constraints per Section 3 (doc §6).</summary>
    public void FreezeTechConstraints(string json)
    {
        TechConstraintsJson = json;
    }

    /// <summary>Set machine-binding mode per doc Sections 4 & 5 (doc §6).</summary>
    public void SetModelBinding(string kind, Guid? modelId, Guid? referenceLineId)
    {
        ModelBindingKind = kind; // "không" | "theo_model" | "như_đơn_cũ"
        PinnedModelId = modelId;
        if (kind == "theo_model") BindingLevel = "bắt_buộc";
        if (kind == "như_đơn_cũ")
        {
            BindingLevel = "ưu_tiên";
            ReferenceOrderLineId = referenceLineId;
        }
        else
        {
            BindingLevel = "bắt_buộc";
            ReferenceOrderLineId = null;
        }
    }

    private OrderItem() { }

    internal static OrderItem FromQuoteItem(Guid orderId, QuoteItem quoteItem) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        ModelId = quoteItem.ModelId,
        Quantity = quoteItem.Quantity,
        Configuration = quoteItem.Configuration,
        UnitPrice = quoteItem.UnitPrice,
        CalculationJson = quoteItem.CalculationJson,
        BatchTotalAmount = quoteItem.BatchTotalAmount,
        QuotedMachineId = quoteItem.QuotedMachineId,
        EstimatedPrintMinutes = quoteItem.EstimatedPrintMinutes,
        EstimatedMaterialGrams = quoteItem.EstimatedMaterialGrams,
        BoundingWidthMm = quoteItem.BoundingWidthMm,
        BoundingDepthMm = quoteItem.BoundingDepthMm,
        BoundingHeightMm = quoteItem.BoundingHeightMm,
        ItemStatus = "Pending"
    };
}
