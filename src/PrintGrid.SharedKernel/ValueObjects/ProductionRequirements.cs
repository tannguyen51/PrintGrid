using System.Security.Cryptography;
using System.Text;

namespace PrintGrid.SharedKernel.ValueObjects;

/// <summary>Customer constraints frozen into the quote and every production/reprint batch.</summary>
public sealed record ProductionRequirements(
    int? MaxColors = null,
    bool? EnclosedChamber = null,
    decimal? NozzleDiameterMm = null,
    decimal? MaxToleranceMm = null,
    decimal? MinLayerHeightMm = null,
    Guid? PinnedModelId = null,
    string? PinnedModelName = null,
    decimal? MinimumBuildWidthMm = null);

public static class MachineModelIdentity
{
    // A model identity is shared across labs, independent of individual machine/profile IDs.
    public static Guid For(string manufacturer, string model)
    {
        static string Normalize(string value) => string.Join(' ', value.Trim().ToUpperInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Normalize(manufacturer)}\n{Normalize(model)}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
