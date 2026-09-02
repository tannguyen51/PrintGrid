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

    private QuoteItem() { }

    internal static QuoteItem Create(
        Guid quoteId,
        Guid modelId,
        int quantity,
        PrintConfiguration configuration,
        Money unitPrice,
        int estimatedPrintMinutes,
        decimal estimatedMaterialGrams)
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
            EstimatedMaterialGrams = estimatedMaterialGrams
        };
    }
}
