using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public static class QuoteMappings
{
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
        quote.Items.Select(i => new QuoteItemDto(
            i.Id,
            i.ModelId,
            i.Quantity,
            i.Configuration.MaterialCode,
            i.Configuration.ColorCode,
            i.Configuration.LayerHeightMm,
            i.Configuration.InfillPercent,
            i.UnitPrice.Amount,
            i.EstimatedPrintMinutes,
            i.EstimatedMaterialGrams)).ToList());
}