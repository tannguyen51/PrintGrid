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

    /// <summary>Part bounding box (mm) from geometry analysis — carried to order/jobs for real placement (FR-SCHED-006).</summary>
    public decimal? BoundingWidthMm { get; private set; }
    public decimal? BoundingDepthMm { get; private set; }
    public decimal? BoundingHeightMm { get; private set; }

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
