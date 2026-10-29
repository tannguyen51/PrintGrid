using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Modules.Admin.Application.Samples;

/// <summary>
/// Public price presentation (FR-CUST-015). The engine computes the exact number; the
/// anonymous view only ever receives a widened, 10.000đ-rounded band, so no raw unit
/// price can be back-solved from the response.
/// </summary>
public static class PublicPricing
{
    /// <summary>Reference part the public pricing page quotes: 50 cm³, ~40 mm tall.</summary>
    public const decimal ReferenceVolumeCm3 = 50m;
    public const decimal ReferenceInfillPercent = 30m;
    public const decimal ReferenceLayerHeightMm = 0.2m;
    public const decimal BandLowerFactor = 0.9m;
    public const decimal BandUpperFactor = 1.1m;
    private const decimal Rounding = 10_000m;

    public static PublicPriceRangeDto Band(decimal exact)
    {
        var from = decimal.Floor(exact * BandLowerFactor / Rounding) * Rounding;
        var to = decimal.Ceiling(exact * BandUpperFactor / Rounding) * Rounding;
        return new PublicPriceRangeDto(from, decimal.Max(to, from + Rounding), "VND");
    }

    public static decimal ExactUnitPrice(
        PricingParameterSet set, string materialCode, decimal volumeCm3, int baseMinutes) =>
        QuotePricing.PriceLine(
            set, volumeCm3, baseMinutes, ReferenceInfillPercent, materialCode, ReferenceLayerHeightMm).UnitPrice;

    /// <summary>Cheapest material for a known geometry — what a "từ … đ" card has to show.</summary>
    public static decimal? CheapestExact(PricingParameterSet set, decimal? volumeCm3, int? baseMinutes)
    {
        if (volumeCm3 is null or <= 0 || baseMinutes is null || baseMinutes <= 0) return null;

        decimal? cheapest = null;
        foreach (var materialCode in set.PricePerGram.Keys)
        {
            var price = ExactUnitPrice(set, materialCode, volumeCm3.Value, baseMinutes.Value);
            if (cheapest is null || price < cheapest) cheapest = price;
        }

        return cheapest;
    }

    public static PublicPriceRangeDto? CheapestRange(PricingParameterSet set, decimal? volumeCm3, int? baseMinutes) =>
        CheapestExact(set, volumeCm3, baseMinutes) is { } exact ? Band(exact) : null;
}

public sealed record PublicPriceRangeDto(decimal From, decimal To, string Currency);

public sealed record SampleSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string Category,
    IReadOnlyList<string> Tags,
    string FileFormat,
    long SizeBytes,
    string? CoverImageUrl,
    decimal? VolumeCm3,
    decimal? BoundingWidthMm,
    decimal? BoundingDepthMm,
    decimal? BoundingHeightMm,
    int? EstimatedPrintMinutes,
    PublicPriceRangeDto? PriceRange);

public sealed record SampleDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string Category,
    IReadOnlyList<string> Tags,
    string FileName,
    string FileFormat,
    long SizeBytes,
    string? CoverImageUrl,
    decimal? VolumeCm3,
    decimal? BoundingWidthMm,
    decimal? BoundingDepthMm,
    decimal? BoundingHeightMm,
    int? EstimatedPrintMinutes,
    decimal? EstimatedMaterialGrams,
    PublicPriceRangeDto? PriceRange,
    DateTime CreatedAtUtc);

/// <summary>Admin view — includes curation state and the stored file location.</summary>
public sealed record SampleAdminDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string Category,
    IReadOnlyList<string> Tags,
    string FileName,
    string FileFormat,
    long SizeBytes,
    string? CoverImageUrl,
    string StorageKey,
    decimal? VolumeCm3,
    decimal? BoundingWidthMm,
    decimal? BoundingDepthMm,
    decimal? BoundingHeightMm,
    int? EstimatedPrintMinutes,
    decimal? EstimatedMaterialGrams,
    bool IsPublished,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public static class SampleMappings
{
    public static SampleSummaryDto ToSummary(SampleModel sample, PricingParameterSet pricing) => new(
        sample.Id, sample.Slug, sample.Name, sample.Description, sample.Category,
        sample.Tags.ToList(), sample.FileFormat, sample.SizeBytes, sample.CoverImageUrl,
        sample.VolumeCm3, sample.BoundingWidthMm, sample.BoundingDepthMm, sample.BoundingHeightMm,
        sample.EstimatedPrintMinutes,
        PublicPricing.CheapestRange(pricing, sample.VolumeCm3, sample.EstimatedPrintMinutes));

    public static SampleDetailDto ToDetail(SampleModel sample, PricingParameterSet pricing) => new(
        sample.Id, sample.Slug, sample.Name, sample.Description, sample.Category,
        sample.Tags.ToList(), sample.FileName, sample.FileFormat, sample.SizeBytes,
        sample.CoverImageUrl, sample.VolumeCm3, sample.BoundingWidthMm, sample.BoundingDepthMm,
        sample.BoundingHeightMm, sample.EstimatedPrintMinutes, sample.EstimatedMaterialGrams,
        PublicPricing.CheapestRange(pricing, sample.VolumeCm3, sample.EstimatedPrintMinutes),
        sample.CreatedAtUtc);

    public static SampleAdminDto ToAdmin(SampleModel sample) => new(
        sample.Id, sample.Slug, sample.Name, sample.Description, sample.Category,
        sample.Tags.ToList(), sample.FileName, sample.FileFormat, sample.SizeBytes,
        sample.CoverImageUrl, sample.StorageKey, sample.VolumeCm3, sample.BoundingWidthMm,
        sample.BoundingDepthMm, sample.BoundingHeightMm, sample.EstimatedPrintMinutes,
        sample.EstimatedMaterialGrams, sample.IsPublished, sample.SortOrder,
        sample.CreatedAtUtc, sample.UpdatedAtUtc);
}
