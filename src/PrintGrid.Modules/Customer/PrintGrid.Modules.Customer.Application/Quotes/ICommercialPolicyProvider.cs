using PrintGrid.SharedKernel;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public sealed record CommercialPolicy(
    PricingParameterSet Pricing,
    decimal AutoLaneThreshold,
    int QuoteValidityHours,
    LabScoreParameters LabScoreParameters);

public interface ICommercialPolicyProvider
{
    Task<CommercialPolicy> GetActiveAsync(CancellationToken cancellationToken = default);
}

public sealed class DefaultCommercialPolicyProvider : ICommercialPolicyProvider
{
    public Task<CommercialPolicy> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CommercialPolicy(PricingParameterSet.Active, 300_000m, 48, new LabScoreParameters()));
}
