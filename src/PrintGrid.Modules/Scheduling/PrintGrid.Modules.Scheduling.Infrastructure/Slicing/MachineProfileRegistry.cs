using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

public sealed class MachineProfileRegistry(PrintGridDbContext db, IConfiguration configuration) : IMachineProfileMatcher, ICuraProfileRegistry
{
    private string EngineVersion => configuration["QuoteSlicer:EngineVersion"] ?? "5.11.0";
    public async Task MatchAsync(Machine machine, CancellationToken cancellationToken)
    {
        var candidates = await db.Set<CuraProfile>().Where(p => CuraProfile.IsActive && p.EngineVersion == EngineVersion)
            .OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id).ToListAsync(cancellationToken);
        var matches = candidates.Where(p => p.Matches(machine)).GroupBy(p => p.MaterialCode)
            .Select(g => g.First()).ToList();
        machine.SetMatchedProfiles(matches.Select(p => p.Id), machine.SupportedMaterials.Count > 0 &&
            machine.SupportedMaterials.All(m => matches.Any(p => p.MaterialCode == m)));
    }

    public async Task<IReadOnlyList<Guid>> GetApprovedMachineIdsAsync(CancellationToken cancellationToken) =>
        await db.Set<Machine>().AsNoTracking().Where(m => m.ProfileStatus == "Approved" && m.PrinterConfiguration != null)
            .Select(m => m.Id).ToListAsync(cancellationToken);

    public async Task<RegisteredCuraSettings?> ResolveAsync(Guid machineId, string material, CancellationToken cancellationToken)
    {
        var machine = await db.Set<Machine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        if (machine?.ProfileStatus != "Approved" || machine.PrinterConfiguration is not { } config) return null;
        var profiles = await db.Set<CuraProfile>().AsNoTracking()
            .Where(p => machine.CuraProfileIds.Contains(p.Id)).ToListAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.EngineVersion == EngineVersion && p.MaterialCode.Equals(material, StringComparison.InvariantCultureIgnoreCase) && p.Matches(machine));
        return profile is null ? null : new RegisteredCuraSettings(profile.SettingsJson, config.MaximumColorSlots, config.PricePerHour, config.FailureRate);
    }
}
