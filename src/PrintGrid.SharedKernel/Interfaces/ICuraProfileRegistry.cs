namespace PrintGrid.SharedKernel.Interfaces;

public sealed record RegisteredCuraSettings(string SettingsJson, int MaximumColorSlots,
    decimal PricePerHour, decimal FailureRate);

public interface ICuraProfileRegistry
{
    Task<IReadOnlyList<Guid>> GetApprovedMachineIdsAsync(CancellationToken cancellationToken);
    Task<RegisteredCuraSettings?> ResolveAsync(Guid machineId, string material, CancellationToken cancellationToken);
}
