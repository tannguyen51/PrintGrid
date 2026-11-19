using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.SharedKernel;

namespace PrintGrid.Modules.Admin.Infrastructure;

/// <summary>Reads the active DB version for each new quote; existing quotes retain their version.</summary>
public sealed class CommercialPolicyProvider(PrintGridDbContext context) : ICommercialPolicyProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CommercialPolicy> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var active = await context.Set<CommercialConfigurationVersion>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Status == ConfigurationStatuses.Active, cancellationToken);

        // If the active version was scheduled in the future (should not happen after materialization),
        // treat it as not yet active so callers keep using the previous policy until the scheduled time.
        if (active is not null && active.ActivatedAtUtc.HasValue && active.ActivatedAtUtc.Value > now)
        {
            active = null;
        }

        // Lazy materialization: a scheduled version whose time has come becomes active now.
        var scheduled = await context.Set<CommercialConfigurationVersion>()
            .Where(x => x.Status == ConfigurationStatuses.Draft && x.ScheduledActivationAtUtc.HasValue && x.ScheduledActivationAtUtc.Value <= now)
            .OrderByDescending(x => x.ScheduledActivationAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (scheduled is not null)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            // Re-read with tracking inside the transaction.
            var currentActive = await context.Set<CommercialConfigurationVersion>()
                .FirstOrDefaultAsync(x => x.Status == ConfigurationStatuses.Active, cancellationToken);
            currentActive?.Archive();

            var toActivate = await context.Set<CommercialConfigurationVersion>().FirstAsync(x => x.Id == scheduled.Id, cancellationToken);
            toActivate.MaterializeScheduledActivation(now);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            active = toActivate;
        }

        if (active is null)
            return new CommercialPolicy(PricingParameterSet.Active, 300_000m, 48, new LabScoreParameters());

        var parameters = JsonSerializer.Deserialize<CommercialParametersDto>(active.ParametersJson, _jsonOptions)!;
        var pricing = new PricingParameterSet(
            $"commercial-v{active.VersionNumber}",
            new Dictionary<string, decimal>(parameters.MaterialRates, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, decimal>(parameters.MaterialDensities, StringComparer.OrdinalIgnoreCase),
            parameters.PricePerPrintHour,
            PricingParameterSet.Active.TimeMultiplierByLayer,
            parameters.ColorPricePerKg, parameters.FailureRate, parameters.PolishingPerUnit);

        var labScoreParameters = new LabScoreParameters(
            parameters.LabProvisionalBaseScore,
            parameters.LabProvisionalWeightK,
            parameters.LabProvisionalThreshold);

        return new CommercialPolicy(pricing, parameters.AutoLaneThreshold, parameters.QuoteValidityHours, labScoreParameters);
    }
}
