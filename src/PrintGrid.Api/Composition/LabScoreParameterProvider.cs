using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.SharedKernel;

namespace PrintGrid.Api.Composition;

/// <summary>
/// Bridges the active commercial configuration into the scheduling lab-score engine.
/// Falls back to sensible defaults when no active commercial configuration exists.
/// </summary>
public sealed class CommercialLabScoreParameterProvider(ICommercialPolicyProvider policy) : ILabScoreParameterProvider
{
    public async Task<LabScoreParameters> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var commercial = await policy.GetActiveAsync(cancellationToken);
            return commercial.LabScoreParameters;
        }
        catch
        {
            return new LabScoreParameters();
        }
    }
}
