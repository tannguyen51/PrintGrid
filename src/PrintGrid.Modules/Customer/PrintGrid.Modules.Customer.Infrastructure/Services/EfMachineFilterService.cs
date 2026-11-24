using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Infrastructure.Services;

public sealed class EfMachineFilterService(PrintGridDbContext db, IConfiguration? configuration = null) : IMachineFilterService
{
    private async Task<List<Machine>> ActiveMachines(CancellationToken ct)
    {
        var labs = await db.Set<Lab>().AsNoTracking().Include(l => l.Machines).Include(l => l.MaterialStocks)
            .Where(l => l.IsActive && l.Status == LabStatus.Active && l.IsInGoodStanding)
            .ToListAsync(ct);
        return labs.Where(l => l.IsDeclarationComplete).SelectMany(l => l.Machines).Where(m => m.ProfileStatus == "Approved"
            && m.PrinterConfiguration is not null && m.Status is MachineStatus.Idle or MachineStatus.Printing
            && !m.IsInMaintenanceAt(DateTime.UtcNow)).ToList();
    }

    private static Guid ModelId(Machine m) => MachineModelIdentity.For(m.PrinterConfiguration!.Manufacturer, m.Model);
    private static bool Matches(Machine m, TechConstraints? tc) => tc is null || tc.Value.MatchesMachine(
        m.PrinterConfiguration!.MaximumColorSlots, m.PrinterConfiguration.EnclosedChamber,
        m.PrinterConfiguration.NozzleDiameterMm, m.AchievableToleranceMm, m.MinLayerHeightMm);

    public async Task<IReadOnlyList<Guid>> FilterByTechConstraintsAsync(IReadOnlyList<Guid> candidates,
        TechConstraints? constraints, CancellationToken ct) => (await ActiveMachines(ct))
        .Where(m => candidates.Contains(m.Id) && Matches(m, constraints)).Select(m => m.Id).ToList();

    public async Task<(IReadOnlyList<Guid> pinnedIds, IReadOnlyList<Guid> unpinnedFallback)> FilterByPinModelAsync(
        Guid modelId, IReadOnlyList<Guid> candidates, CancellationToken ct) =>
        ((await ActiveMachines(ct)).Where(m => candidates.Contains(m.Id) && m.AllowsCustomerPinning && ModelId(m) == modelId)
            .Select(m => m.Id).ToList(), candidates);

    public async Task<IReadOnlyList<Guid>> GetEligiblePinModelsAsync(string materialCode, int maxColors,
        bool? enclosedChamber, decimal? nozzleDiameterMm, decimal? maxToleranceMm, decimal? minLayerHeightMm,
        int minMachines = 3, int minLabs = 2, CancellationToken ct = default) =>
        (await GetOptionsAsync(new MachineSelectionCriteria(materialCode, [], 0, 0, 0, 1, 100,
            TechConstraints.From(maxColors, enclosedChamber, nozzleDiameterMm, maxToleranceMm, minLayerHeightMm)),
            minMachines, minLabs, 1.15m, ct)).Models.Select(m => m.Id).ToList();

    private async Task<List<Machine>> Candidates(MachineSelectionCriteria c, CancellationToken ct)
    {
        c = c with { Colors = c.Colors.Select(MaterialColorCodes.Normalize).Distinct().ToList() };
        var machines = (await ActiveMachines(ct)).Where(m => m.SupportedMaterials.Contains(c.MaterialCode)
            && m.BuildVolume.WidthMm >= c.WidthMm && m.BuildVolume.DepthMm >= c.DepthMm && m.BuildVolume.HeightMm >= c.HeightMm
            && m.MinLayerHeightMm <= c.LayerHeightMm && m.AchievableToleranceMm <= c.ToleranceMm
            && m.PrinterConfiguration!.MaximumColorSlots >= c.Colors.Count && Matches(m, c.TechConstraints)).ToList();
        var labIds = machines.Select(m => m.LabId).Distinct().ToList();
        var profiles = await db.Set<CuraProfile>().AsNoTracking().Where(p => p.IsActive
            && p.ApprovalStatus == CuraProfile.StatusApproved && p.MaterialCode == c.MaterialCode)
            .ToListAsync(ct);
        var engineVersion = configuration?["QuoteSlicer:EngineVersion"] ?? "5.11.0";
        var labs = await db.Set<Lab>().AsNoTracking().Include(l => l.MaterialStocks)
            .Where(l => labIds.Contains(l.Id)).ToListAsync(ct);
        return machines.Where(m => profiles.Any(p => p.EngineVersion == engineVersion && m.CuraProfileIds.Contains(p.Id) && p.Matches(m))
            && c.Colors.All(color => labs.Single(l => l.Id == m.LabId).MaterialStocks.Any(s =>
                s.MaterialCode == c.MaterialCode && MaterialColorCodes.Equivalent(s.ColorCode, color) && s.AssignableGrams > 0))).ToList();
    }

    public async Task<IReadOnlyList<Guid>> FilterCandidatesAsync(IReadOnlyList<Guid> candidates,
        MachineSelectionCriteria criteria, CancellationToken ct) => (await Candidates(criteria, ct))
        .Where(m => candidates.Contains(m.Id)).Select(m => m.Id).ToList();

    public async Task<MachineModelOptions> GetOptionsAsync(MachineSelectionCriteria criteria, int minMachines,
        int minLabs, decimal pinCoefficient, CancellationToken ct)
    {
        var candidates = await Candidates(criteria, ct);
        var disabled = (await db.Set<Machine>().AsNoTracking()
            .Where(m => !m.AllowsCustomerPinning && m.PrinterConfiguration != null).ToListAsync(ct)).Select(ModelId).ToHashSet();
        var models = candidates.GroupBy(ModelId)
            .Where(g => !disabled.Contains(g.Key) && g.Count() >= minMachines
                && g.Select(m => m.LabId).Distinct().Count() >= minLabs)
            .Select(g => new MachineModelOption(g.Key,
                $"{g.First().PrinterConfiguration!.Manufacturer} · {g.First().Model}", g.Count()))
            .OrderBy(m => m.Name).ToList();
        return new(models, candidates.Count, pinCoefficient);
    }

    public async Task<QuoteProfileSource?> GetReferenceMachineAsync(IReadOnlyList<Guid> candidates, string materialCode,
        Guid? referenceProfileId, CancellationToken ct)
    {
        // An explicitly configured reference is an estimation resource; its source machine may
        // be offline without preventing production on other capable machines in the network.
        var machines = referenceProfileId.HasValue
            ? await db.Set<Machine>().AsNoTracking().Where(m => m.ProfileStatus == "Approved" && m.PrinterConfiguration != null).ToListAsync(ct)
            : (await ActiveMachines(ct)).Where(m => candidates.Contains(m.Id)).ToList();
        var engineVersion = configuration?["QuoteSlicer:EngineVersion"] ?? "5.11.0";
        var profiles = await db.Set<CuraProfile>().AsNoTracking()
            .Where(p => p.IsActive && p.ApprovalStatus == CuraProfile.StatusApproved
                && p.MaterialCode == materialCode && p.EngineVersion == engineVersion)
            .OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id).ToListAsync(ct);
        if (referenceProfileId.HasValue) profiles = profiles.Where(p => p.Id == referenceProfileId).ToList();
        foreach (var profile in profiles)
        {
            var machine = machines.FirstOrDefault(m => m.CuraProfileIds.Contains(profile.Id) && profile.Matches(m));
            if (machine is not null) return new(machine.Id, profile.Id);
        }
        return null;
    }

    public async Task<decimal> GetCalibrationFactorAsync(Guid? modelId, CancellationToken ct)
    {
        var machines = (await ActiveMachines(ct)).Where(m => !modelId.HasValue || ModelId(m) == modelId).ToList();
        var learned = machines.Where(m => m.CalibrationSampleCount > 0).ToList();
        return learned.Count == 0 ? 1m : learned.Sum(m => m.CalibrationFactor * m.CalibrationSampleCount)
            / learned.Sum(m => m.CalibrationSampleCount);
    }
}
