using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.ValueObjects;

public sealed class PrintConfiguration : ValueObject
{
    public string MaterialCode { get; }
    public string ColorCode { get; }
    public decimal LayerHeightMm { get; }
    public int InfillPercent { get; }
    public decimal ToleranceMm { get; }

    private PrintConfiguration(
        string materialCode,
        string colorCode,
        decimal layerHeightMm,
        int infillPercent,
        decimal toleranceMm)
    {
        MaterialCode = materialCode;
        ColorCode = colorCode;
        LayerHeightMm = layerHeightMm;
        InfillPercent = infillPercent;
        ToleranceMm = toleranceMm;
    }

    public static PrintConfiguration Create(
        string materialCode,
        string colorCode,
        decimal layerHeightMm,
        int infillPercent,
        decimal toleranceMm)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
            throw new ArgumentException("Material is required", nameof(materialCode));
        if (layerHeightMm is < 0.05m or > 0.4m)
            throw new ArgumentOutOfRangeException(nameof(layerHeightMm), "Layer height must be between 0.05 and 0.4 mm");
        if (infillPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(infillPercent), "Infill must be between 0 and 100 percent");
        if (toleranceMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(toleranceMm), "Tolerance must be positive");

        return new PrintConfiguration(
            materialCode.Trim().ToUpperInvariant(),
            colorCode.Trim().ToUpperInvariant(),
            layerHeightMm,
            infillPercent,
            toleranceMm);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaterialCode;
        yield return ColorCode;
        yield return LayerHeightMm;
        yield return InfillPercent;
        yield return ToleranceMm;
    }
}
