using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public static class QuoteMappings
{
    /// <summary>
    /// Customer projection: the internal placement basis (lab + machine identity) is
    /// deliberately blanked. The customer only sees district/city via CustomerPlacementSummary.
    /// </summary>
    public static QuoteDto ToCustomerDto(Domain.Entities.Quote quote) =>
        ToDto(quote) with { PlacementBasis = null };

    public static QuoteDto ToDto(Domain.Entities.Quote quote) => new(
        quote.Id,
        quote.CustomerId,
        quote.Status.ToString(),
        quote.TotalPrice.Amount,
        quote.TotalPrice.Currency,
        quote.PromisedDeliveryDate,
        quote.CreatedAt,
        quote.ExpiresAt,
        quote.FailureReason,
        quote.PricingVersion,
        quote.PlacementBasis,
        quote.CustomerPlacementSummary,
        quote.ApprovedAt,
        quote.ReviewedBy,
        quote.AutoApproved,
        quote.EngineTotalAmount,
        quote.EnginePromisedDeliveryDate,
        quote.AdjustmentReason,
        quote.Items.Select(i => new QuoteItemDto(
            i.Id,
            i.ModelId,
            i.Quantity,
            i.Configuration.MaterialCode,
            i.Configuration.ColorCode,
            i.Configuration.LayerHeightMm,
            i.Configuration.InfillPercent,
            i.UnitPrice.Amount,
            i.MaterialCostAmount,
            i.MachineTimeCostAmount,
            i.EstimatedPrintMinutes,
            i.EstimatedMaterialGrams,
            i.CalculationJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<QuotePriceBreakdown>(i.CalculationJson))).ToList(),
        // Section 4 pinned model comparison pricing (BR-06)
        quote.UnpinnedPriceVnd,
        quote.UnpinnedDeliveryDay,
        quote.PinSurchargeCoefficient,
        quote.PinnedModelMachineId.HasValue);
}
