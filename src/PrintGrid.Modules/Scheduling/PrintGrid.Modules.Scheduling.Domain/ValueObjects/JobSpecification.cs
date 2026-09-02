using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.ValueObjects;

public sealed class JobSpecification : ValueObject
{
    public BuildVolume RequiredVolume { get; } = null!;
    public string MaterialCode { get; } = string.Empty;
    public string ColorCode { get; } = string.Empty;
    public decimal LayerHeightMm { get; }
    public decimal ToleranceMm { get; }
    public PrintTechnology Technology { get; }
    public decimal MaterialGrams { get; }

    private JobSpecification() { }

    private JobSpecification(
        BuildVolume requiredVolume,
        string materialCode,
        string colorCode,
        decimal layerHeightMm,
        decimal toleranceMm,
        PrintTechnology technology,
        decimal materialGrams)
    {
        RequiredVolume = requiredVolume;
        MaterialCode = materialCode;
        ColorCode = colorCode;
        LayerHeightMm = layerHeightMm;
        ToleranceMm = toleranceMm;
        Technology = technology;
        MaterialGrams = materialGrams;
    }

    public static JobSpecification Create(
        BuildVolume requiredVolume,
        string materialCode,
        string colorCode,
        decimal layerHeightMm,
        decimal toleranceMm,
        PrintTechnology technology,
        decimal materialGrams)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
            throw new ArgumentException("Material is required", nameof(materialCode));
        if (materialGrams <= 0)
            throw new ArgumentOutOfRangeException(nameof(materialGrams), "Material usage must be positive");

        return new JobSpecification(
            requiredVolume,
            materialCode.Trim().ToUpperInvariant(),
            colorCode.Trim().ToUpperInvariant(),
            layerHeightMm,
            toleranceMm,
            technology,
            materialGrams);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RequiredVolume;
        yield return MaterialCode;
        yield return ColorCode;
        yield return LayerHeightMm;
        yield return ToleranceMm;
        yield return Technology;
        yield return MaterialGrams;
    }
}
