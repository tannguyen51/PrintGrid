using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.ValueObjects;

public sealed class JobSpecification : ValueObject
{
    public BuildVolume RequiredVolume { get; }
    public string MaterialCode { get; }
    public string ColorCode { get; }
    public decimal LayerHeightMm { get; }
    public decimal ToleranceMm { get; }
    public PrintTechnology Technology { get; }
    public decimal MaterialGrams { get; }

    // EF materializes RequiredVolume as an owned navigation after constructing this object.
    private JobSpecification() : this(null!, string.Empty, string.Empty, 0m, 0m, default, 0m) { }

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
