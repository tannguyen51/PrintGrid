using MediatR;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Modules.Admin.Application.Pricing;

/// <summary>
/// Public pricing surface (FR-CUST-015). Two tiers over the SAME parameter set:
/// anonymous callers get computed ranges, customers get the exact table.
/// </summary>
public sealed record PublicPricingDto(
    string Version,
    DateTime? EffectiveAtUtc,
    decimal ReferenceVolumeCm3,
    decimal ReferenceHeightMm,
    decimal ReferenceInfillPercent,
    int ReferenceBaseMinutes,
    IReadOnlyList<PublicMaterialPriceDto> Materials);

public sealed record PublicMaterialPriceDto(
    string Code,
    string Name,
    string? Description,
    bool IsPremium,
    Samples.PublicPriceRangeDto PriceRange);

public sealed record PricingDetailDto(
    string Version,
    DateTime? EffectiveAtUtc,
    decimal PricePerPrintHour,
    decimal ReferenceVolumeCm3,
    decimal ReferenceHeightMm,
    decimal ReferenceInfillPercent,
    int ReferenceBaseMinutes,
    IReadOnlyDictionary<string, decimal> MaterialRates,
    IReadOnlyDictionary<string, decimal> MaterialDensities,
    IReadOnlyList<PricingLayerDto> LayerHeights,
    IReadOnlyList<PricingMaterialDto> Materials,
    PricingExampleDto? ReferenceExample);

/// <summary>
/// One worked example computed by the engine (never by the browser), so the page can
/// show a full breakdown without re-implementing the formula in TypeScript.
/// </summary>
public sealed record PricingExampleDto(
    string MaterialCode,
    decimal VolumeCm3,
    int BaseMinutes,
    int EffectiveMinutes,
    decimal LayerHeightMm,
    decimal InfillPercent,
    decimal Grams,
    decimal MaterialCost,
    decimal MachineCost,
    decimal Total);

public sealed record PricingLayerDto(decimal LayerHeightMm, decimal TimeMultiplier);

public sealed record PricingMaterialDto(
    string Code,
    string Name,
    string? Description,
    decimal UnitRatePerGram,
    decimal DensityGramsPerCm3,
    bool IsPremium);

public sealed record GetPublicPricingQuery(int ReferenceBaseMinutes) : IRequest<PublicPricingDto>;

public sealed record GetPricingDetailQuery(int ReferenceBaseMinutes) : IRequest<PricingDetailDto>;

public sealed class PricingQueryHandlers(
    IAdminConfigurationRepository repository, ICommercialPolicyProvider pricing) :
    IRequestHandler<GetPublicPricingQuery, PublicPricingDto>,
    IRequestHandler<GetPricingDetailQuery, PricingDetailDto>
{
    public async Task<PublicPricingDto> Handle(GetPublicPricingQuery request, CancellationToken cancellationToken)
    {
        var policy = await pricing.GetActiveAsync(cancellationToken);
        var set = policy.Pricing;
        var active = await repository.GetActiveConfigurationAsync(cancellationToken);
        var catalog = await repository.GetCatalogAsync(SharedCatalogTypes.Material, includeDeprecated: false, cancellationToken);

        var materials = catalog
            .Where(item => set.PricePerGram.ContainsKey(item.Code))
            .OrderBy(item => item.IsPremium)
            .ThenBy(item => item.Name)
            .Select(item => new PublicMaterialPriceDto(
                item.Code,
                item.Name,
                item.Description,
                item.IsPremium,
                Samples.PublicPricing.Band(
                    Samples.PublicPricing.ExactUnitPrice(
                        set, item.Code, Samples.PublicPricing.ReferenceVolumeCm3, request.ReferenceBaseMinutes))))
            .ToList();

        // An empty catalogue must still render a usable page — fall back to the engine's codes.
        if (materials.Count == 0)
        {
            materials = set.PricePerGram.Keys
                .OrderBy(code => code)
                .Select(code => new PublicMaterialPriceDto(
                    code, code, null, false,
                    Samples.PublicPricing.Band(
                        Samples.PublicPricing.ExactUnitPrice(
                            set, code, Samples.PublicPricing.ReferenceVolumeCm3, request.ReferenceBaseMinutes))))
                .ToList();
        }

        return new PublicPricingDto(
            set.Version,
            active?.ActivatedAtUtc ?? active?.CreatedAtUtc,
            Samples.PublicPricing.ReferenceVolumeCm3,
            ReferenceHeightMm,
            Samples.PublicPricing.ReferenceInfillPercent,
            request.ReferenceBaseMinutes,
            materials);
    }

    public async Task<PricingDetailDto> Handle(GetPricingDetailQuery request, CancellationToken cancellationToken)
    {
        var policy = await pricing.GetActiveAsync(cancellationToken);
        var set = policy.Pricing;
        var active = await repository.GetActiveConfigurationAsync(cancellationToken);
        var catalog = await repository.GetCatalogAsync(SharedCatalogTypes.Material, includeDeprecated: false, cancellationToken);
        var catalogByCode = catalog.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

        var materials = set.PricePerGram
            .OrderBy(pair => pair.Key)
            .Select(pair =>
            {
                catalogByCode.TryGetValue(pair.Key, out var item);
                return new PricingMaterialDto(
                    pair.Key,
                    item?.Name ?? pair.Key,
                    item?.Description,
                    pair.Value,
                    set.DensityGramsPerCm3.TryGetValue(pair.Key, out var density) ? density : 0m,
                    item?.IsPremium ?? false);
            })
            .ToList();

        var exampleMaterial = set.PricePerGram.ContainsKey("PLA") ? "PLA" : set.PricePerGram.Keys.FirstOrDefault();
        PricingExampleDto? example = null;
        if (exampleMaterial is not null)
        {
            var line = QuotePricing.PriceLine(
                set, Samples.PublicPricing.ReferenceVolumeCm3, request.ReferenceBaseMinutes,
                Samples.PublicPricing.ReferenceInfillPercent, exampleMaterial,
                Samples.PublicPricing.ReferenceLayerHeightMm);

            example = new PricingExampleDto(
                exampleMaterial,
                Samples.PublicPricing.ReferenceVolumeCm3,
                request.ReferenceBaseMinutes,
                line.EffectiveMinutes,
                Samples.PublicPricing.ReferenceLayerHeightMm,
                Samples.PublicPricing.ReferenceInfillPercent,
                line.Grams,
                line.MaterialCostAmount,
                line.MachineTimeCostAmount,
                line.UnitPrice);
        }

        return new PricingDetailDto(
            set.Version,
            active?.ActivatedAtUtc ?? active?.CreatedAtUtc,
            set.PricePerPrintHour,
            Samples.PublicPricing.ReferenceVolumeCm3,
            ReferenceHeightMm,
            Samples.PublicPricing.ReferenceInfillPercent,
            request.ReferenceBaseMinutes,
            new Dictionary<string, decimal>(set.PricePerGram, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, decimal>(set.DensityGramsPerCm3, StringComparer.OrdinalIgnoreCase),
            set.TimeMultiplierByLayer
                .OrderByDescending(pair => pair.Key)
                .Select(pair => new PricingLayerDto(pair.Key, pair.Value))
                .ToList(),
            materials,
            example);
    }

    /// <summary>Reference part the public page quotes: 50 cm³, 40 mm tall (≈ a 40 mm cube).</summary>
    public const decimal ReferenceHeightMm = 40m;
}
