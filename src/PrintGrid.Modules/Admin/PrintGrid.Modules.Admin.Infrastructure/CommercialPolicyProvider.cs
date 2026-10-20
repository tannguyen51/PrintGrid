using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Customer.Application.Quotes;

namespace PrintGrid.Modules.Admin.Infrastructure;

/// <summary>Reads the active DB version for each new quote; existing quotes retain their version.</summary>
public sealed class CommercialPolicyProvider(PrintGridDbContext context) : ICommercialPolicyProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CommercialPolicy> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = await context.Set<CommercialConfigurationVersion>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Status == ConfigurationStatuses.Active, cancellationToken);
        if (active is null)
            return new CommercialPolicy(PricingParameterSet.Active, 300_000m, 48);

        var parameters = JsonSerializer.Deserialize<CommercialParametersDto>(active.ParametersJson, JsonOptions)!;
        var pricing = new PricingParameterSet(
            $"commercial-v{active.VersionNumber}",
            new Dictionary<string, decimal>(parameters.MaterialRates, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, decimal>(parameters.MaterialDensities, StringComparer.OrdinalIgnoreCase),
            parameters.PricePerPrintHour,
            PricingParameterSet.Active.TimeMultiplierByLayer);
        return new CommercialPolicy(pricing, parameters.AutoLaneThreshold, parameters.QuoteValidityHours);
    }
}
