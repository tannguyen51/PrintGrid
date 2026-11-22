using PrintGrid.Modules.Customer.Domain.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Abstractions;

public sealed record MachineModelOption(Guid Id, string Name, int ApprovedMachineCount);
public sealed record QuoteProfileSource(Guid MachineId, Guid ProfileId);
public sealed record MachineSelectionCriteria(string MaterialCode, IReadOnlyList<string> Colors,
    decimal WidthMm, decimal DepthMm, decimal HeightMm, decimal LayerHeightMm, decimal ToleranceMm,
    TechConstraints? TechConstraints = null);
public sealed record MachineModelOptions(IReadOnlyList<MachineModelOption> Models,
    int MatchingMachineCount, decimal PinSurchargeCoefficient);

public interface IMachineFilterService
{
    Task<IReadOnlyList<Guid>> FilterByTechConstraintsAsync(IReadOnlyList<Guid> candidateMachineIds,
        TechConstraints? techConstraints, CancellationToken ct);
    Task<(IReadOnlyList<Guid> pinnedIds, IReadOnlyList<Guid> unpinnedFallback)> FilterByPinModelAsync(
        Guid pinnedModelId, IReadOnlyList<Guid> allCandidateIds, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetEligiblePinModelsAsync(string materialCode, int maxColors,
        bool? enclosedChamber, decimal? nozzleDiameterMm, decimal? maxToleranceMm, decimal? minLayerHeightMm,
        int minMachines = 3, int minLabs = 2, CancellationToken ct = default);
    Task<MachineModelOptions> GetOptionsAsync(MachineSelectionCriteria criteria, int minMachines,
        int minLabs, decimal pinCoefficient, CancellationToken ct);
    Task<IReadOnlyList<Guid>> FilterCandidatesAsync(IReadOnlyList<Guid> candidates,
        MachineSelectionCriteria criteria, CancellationToken ct);
    Task<QuoteProfileSource?> GetReferenceMachineAsync(IReadOnlyList<Guid> candidates, string materialCode,
        Guid? referenceProfileId, CancellationToken ct);
    Task<decimal> GetCalibrationFactorAsync(Guid? modelId, CancellationToken ct);
}
