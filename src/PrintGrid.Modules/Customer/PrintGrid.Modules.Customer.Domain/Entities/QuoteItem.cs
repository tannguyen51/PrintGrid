using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class QuoteItem : Entity<Guid>
{
    public Guid QuoteId { get; private set; }
    public Guid ModelId { get; private set; }
    public int Quantity { get; private set; }
    public PrintConfiguration Configuration { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int EstimatedPrintMinutes { get; private set; }
    public decimal EstimatedMaterialGrams { get; private set; }

    /// <summary>Per-unit cost split frozen with the quote (BR-QUOTE-005 itemized breakdown).</summary>
    public decimal MaterialCostAmount { get; private set; }
    public decimal MachineTimeCostAmount { get; private set; }
    public string? CalculationJson { get; private set; }
    public decimal? BatchTotalAmount { get; private set; }
    public Guid? QuotedMachineId { get; private set; }

    /// <summary>Part bounding box (mm) from geometry analysis — carried to order/jobs for real placement (FR-SCHED-006).</summary>
    public decimal? BoundingWidthMm { get; private set; }
    public decimal? BoundingDepthMm { get; private set; }
    public decimal? BoundingHeightMm { get; private set; }

    /// <summary>Profile used for slicer estimation when customer pins a model (Section 4.2).</summary>
    public Guid? ProfileId { get; internal set; }

    /// <summary>Version string of the frozen profile (for re-quote traceability).</summary>
    public string? ProfileVersion { get; internal set; }

    /// <summary>Model-pinch surcharge coefficient: null = 1.0 (unpinned), otherwise e.g. 1.15 (BR-05).</summary>
    public decimal? SurchargePinchCoef { get; internal set; }

    /// <summary>Alternative unpinned price for display comparison (Section 4.4). Stored in VND.</summary>
    public long? AlternativePriceVnd { get; internal set; }

    /// <summary>Alternative unpinned estimated delivery date (Section 4.4).</summary>
    public DateOnly? AlternativeDeliveryDate { get; internal set; }

    public void FreezeCalculation(string json, decimal batchTotal, Guid? machineId = null)
    {
        if (CalculationJson is not null || batchTotal <= 0) throw new InvalidOperationException("Invalid or already frozen calculation");
        CalculationJson = json;
        BatchTotalAmount = batchTotal;
        QuotedMachineId = machineId;
    }

    /// <summary>Freeze model-pinch data per Section 4.2–4.4 (doc requirement §6 Quote entity).</summary>
    public void FreezePinchData(Guid profileId, string profileVersion, decimal surchargeCoef,
        long alternativePriceVnd, DateOnly alternativeDeliveryDate)
    {
        ProfileId = profileId;
        ProfileVersion = profileVersion;
        SurchargePinchCoef = surchargeCoef;
        AlternativePriceVnd = alternativePriceVnd;
        AlternativeDeliveryDate = alternativeDeliveryDate;
    }

    private QuoteItem() { }

    internal static QuoteItem Create(
        Guid quoteId,
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
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1");

        return new QuoteItem
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            ModelId = modelId,
            Quantity = quantity,
            Configuration = configuration,
            UnitPrice = unitPrice,
            EstimatedPrintMinutes = estimatedPrintMinutes,
            EstimatedMaterialGrams = estimatedMaterialGrams,
            MaterialCostAmount = materialCostAmount,
            MachineTimeCostAmount = machineTimeCostAmount,
            BoundingWidthMm = boundingWidthMm,
            BoundingDepthMm = boundingDepthMm,
            BoundingHeightMm = boundingHeightMm
        };
    }
}
