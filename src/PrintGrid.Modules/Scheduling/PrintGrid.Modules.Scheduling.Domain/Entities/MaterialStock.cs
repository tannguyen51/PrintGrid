using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class MaterialStock : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public string ColorCode { get; private set; } = string.Empty;
    public decimal AvailableGrams { get; private set; }
    public decimal ReservedGrams { get; private set; }
    public decimal ReorderPointGrams { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public decimal AssignableGrams => AvailableGrams - ReservedGrams;
    public bool IsLowStock => AvailableGrams <= ReorderPointGrams;

    private MaterialStock() { }

    internal static MaterialStock Create(Guid labId, string materialCode, string colorCode, decimal availableGrams, decimal reorderPointGrams = 0m)
    {
        if (availableGrams < 0) throw new ArgumentOutOfRangeException(nameof(availableGrams));
        if (reorderPointGrams < 0) throw new ArgumentOutOfRangeException(nameof(reorderPointGrams));
        return new MaterialStock
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            MaterialCode = Normalize(materialCode),
            ColorCode = Normalize(colorCode),
            AvailableGrams = availableGrams,
            ReorderPointGrams = reorderPointGrams,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    internal void ConfigureReorderPoint(decimal reorderPointGrams)
    {
        if (reorderPointGrams < 0) throw new ArgumentOutOfRangeException(nameof(reorderPointGrams));
        ReorderPointGrams = reorderPointGrams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void Adjust(decimal deltaGrams)
    {
        if (AvailableGrams + deltaGrams < ReservedGrams) throw new InvalidOperationException("Stock cannot be lower than its reserved quantity");
        AvailableGrams += deltaGrams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void Reserve(decimal grams)
    {
        if (grams <= 0 || AssignableGrams < grams) throw new InvalidOperationException("Insufficient assignable stock");
        ReservedGrams += grams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void Release(decimal grams)
    {
        if (grams <= 0 || ReservedGrams < grams) throw new InvalidOperationException("Invalid stock release");
        ReservedGrams -= grams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void Settle(decimal reservedGrams, decimal actualGrams)
    {
        if (actualGrams <= 0 || ReservedGrams < reservedGrams || AvailableGrams < actualGrams)
            throw new InvalidOperationException("Invalid stock settlement");
        ReservedGrams -= reservedGrams;
        AvailableGrams -= actualGrams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stock code is required") : value.Trim().ToUpperInvariant();
}
