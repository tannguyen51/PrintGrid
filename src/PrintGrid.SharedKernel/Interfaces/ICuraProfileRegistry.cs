namespace PrintGrid.SharedKernel.Interfaces;

public sealed record RegisteredCuraSettings(string SettingsJson, int MaximumColorSlots,
    decimal PricePerHour, decimal FailureRate,
    Guid? ProfileId = null, string? ProfileVersion = null,
    decimal BuildWidthMm = 0, decimal BuildDepthMm = 0);

public interface ICuraProfileRegistry
{
    Task<IReadOnlyList<Guid>> GetApprovedMachineIdsAsync(CancellationToken cancellationToken);
    Task<RegisteredCuraSettings?> ResolveAsync(Guid machineId, string material, CancellationToken cancellationToken, Guid? profileId = null);
}
