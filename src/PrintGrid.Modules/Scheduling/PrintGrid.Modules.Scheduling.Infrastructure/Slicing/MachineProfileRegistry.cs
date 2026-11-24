using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

public sealed class MachineProfileRegistry(PrintGridDbContext db, IConfiguration configuration) : IMachineProfileMatcher, ICuraProfileRegistry
{
    private string EngineVersion => configuration["QuoteSlicer:EngineVersion"] ?? "5.11.0";
    public async Task MatchAsync(Machine machine, CancellationToken cancellationToken)
    {
        var candidates = await db.Set<CuraProfile>()
            .Where(p => p.IsActive && p.ApprovalStatus == CuraProfile.StatusApproved && p.EngineVersion == EngineVersion)
            .OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id).ToListAsync(cancellationToken);
        var matches = candidates.Where(p => p.Matches(machine)).GroupBy(p => p.MaterialCode)
            .Select(g => g.First()).ToList();
        var complete = machine.SupportedMaterials.Count > 0 &&
            machine.SupportedMaterials.All(m => matches.Any(p => p.MaterialCode == m));
        var autoApprove = configuration.GetValue("CuraLibrary:AutoApproveVerifiedExactMatches", false)
                          && complete && matches.All(p => p.IsVerified)
                          && !string.IsNullOrWhiteSpace(machine.PrinterConfiguration?.CuraDefinitionId);
        machine.SetVerifiedProfiles(matches.Select(p => p.Id), complete, autoApprove);
    }

    public async Task<IReadOnlyList<Guid>> GetApprovedMachineIdsAsync(CancellationToken cancellationToken) =>
        await db.Set<Machine>().AsNoTracking()
            .Where(m => m.ProfileStatus == "Approved"
                        && m.PrinterConfiguration != null
                        && m.CuraProfileIds.Length > 0
                        && m.Status != MachineStatus.Offline
                        && m.Status != MachineStatus.Maintenance)
            .Select(m => m.Id).ToListAsync(cancellationToken);

    public async Task<RegisteredCuraSettings?> ResolveAsync(Guid machineId, string material, CancellationToken cancellationToken, Guid? profileId = null)
    {
        var machine = await db.Set<Machine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine?.ProfileStatus != "Approved"
            || machine.PrinterConfiguration is not { } config
            || machine.Status is MachineStatus.Offline or MachineStatus.Maintenance)
            return null;
        var profiles = await db.Set<CuraProfile>().AsNoTracking()
            .Where(p => machine.CuraProfileIds.Contains(p.Id)
                        && p.IsActive
                        && p.ApprovalStatus == CuraProfile.StatusApproved)
            .ToListAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => (!profileId.HasValue || p.Id == profileId) && p.EngineVersion == EngineVersion
            && p.MaterialCode.Equals(material, StringComparison.InvariantCultureIgnoreCase) && p.Matches(machine));
        return profile is null ? null : new RegisteredCuraSettings(profile.SettingsJson, config.MaximumColorSlots,
            config.PricePerHour, config.FailureRate, profile.Id, profile.Version, profile.WidthMm, profile.DepthMm);
    }
}
