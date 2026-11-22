using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public sealed record DeliveryAssessment(DateOnly EarliestDeliveryDate, DateOnly RequestedDeliveryDate,
    DateOnly PromisedDeliveryDate, bool CanMeetRequestedDate, int LastMileDays);

/// <summary>Re-check live production capacity before promising delivery; quote dates are hub-ready estimates.</summary>
public sealed class OrderDeliveryService(IProductionCapacityProbe capacity, IDateTimeProvider clock, IConfiguration configuration)
{
    public async Task<Result<DeliveryAssessment>> AssessAsync(Quote quote, DateOnly requested, CancellationToken ct)
    {
        if (requested <= clock.Today)
            return Result.Failure<DeliveryAssessment>(Error.Validation("Ngày mong muốn phải sau ngày hiện tại."));
        var lastMileDays = int.TryParse(configuration["Delivery:LastMileDays"], out var days) ? Math.Clamp(days, 1, 30) : 2;
        var hubReady = quote.PromisedDeliveryDate;
        // Group by the frozen machine: different lines on the same machine must be trialled serially.
        foreach (var group in quote.Items.GroupBy(i => i.QuotedMachineId))
        {
            var lines = new List<TrialPlacementLine>();
            foreach (var item in group)
            {
                ProductionRequirements? requirements = null;
                var colors = new Dictionary<string, decimal> { [item.Configuration.ColorCode] = item.EstimatedMaterialGrams * item.Quantity };
                if (item.CalculationJson is not null)
                {
                    using var json = JsonDocument.Parse(item.CalculationJson);
                    colors = json.RootElement.GetProperty("Colors").EnumerateArray().ToDictionary(
                        c => c.GetProperty("ColorCode").GetString()!, c => c.GetProperty("Grams").GetDecimal());
                    if (json.RootElement.TryGetProperty("Requirements", out var frozen) && frozen.ValueKind == JsonValueKind.Object)
                        requirements = JsonSerializer.Deserialize<ProductionRequirements>(frozen);
                }
                var first = true;
                foreach (var color in colors)
                {
                    lines.Add(new TrialPlacementLine(item.BoundingWidthMm ?? 120, item.BoundingDepthMm ?? 120,
                        item.BoundingHeightMm ?? 120, item.Configuration.MaterialCode, color.Key,
                        item.Configuration.LayerHeightMm, item.Configuration.ToleranceMm,
                        first ? Math.Max(1, item.EstimatedPrintMinutes * item.Quantity) : 0, color.Value,
                        requirements is null ? item.QuotedMachineId : null, requirements, requirements is not null));
                    first = false;
                }
            }
            var trial = await capacity.FindEarliestFeasibleDeliveryAsync(lines, clock.UtcNow, ct);
            if (trial is null)
                return Result.Failure<DeliveryAssessment>(Error.Conflict("Hiện chưa đủ năng lực sản xuất. Vui lòng yêu cầu báo giá mới hoặc liên hệ điều phối."));
            if (trial.PromisedDeliveryDate > hubReady) hubReady = trial.PromisedDeliveryDate;
        }
        var earliest = hubReady.AddDays(lastMileDays);
        return Result.Success(new DeliveryAssessment(earliest, requested, requested, requested >= earliest, lastMileDays));
    }
}
