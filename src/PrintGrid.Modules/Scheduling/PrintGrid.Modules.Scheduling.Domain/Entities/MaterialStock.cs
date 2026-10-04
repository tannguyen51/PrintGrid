using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

public class MaterialStock : Entity<Guid>
{
    public Guid LabId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public string ColorCode { get; private set; } = string.Empty;
    public decimal AvailableGrams { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private MaterialStock() { }

    internal static MaterialStock Create(Guid labId, string materialCode, string colorCode, decimal availableGrams)
    {
        if (availableGrams < 0) throw new ArgumentOutOfRangeException(nameof(availableGrams));
        return new MaterialStock
        {
            Id = Guid.NewGuid(),
            LabId = labId,
            MaterialCode = Normalize(materialCode),
            ColorCode = Normalize(colorCode),
            AvailableGrams = availableGrams,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    internal void SetAvailable(decimal availableGrams)
    {
        if (availableGrams < 0) throw new ArgumentOutOfRangeException(nameof(availableGrams));
        AvailableGrams = availableGrams;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stock code is required") : value.Trim().ToUpperInvariant();
}
