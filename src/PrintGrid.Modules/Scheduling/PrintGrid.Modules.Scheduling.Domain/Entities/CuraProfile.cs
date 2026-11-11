using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>Immutable, resolved Cura settings for one hardware/material/quality combination.</summary>
public sealed class CuraProfile : Entity<Guid>
{
    public string Name { get; private set; } = "";
    public string Manufacturer { get; private init; } = "";
    public string MachineModel { get; private init; } = "";
    public string MaterialCode { get; private init; } = "";
    public string Version { get; private set; } = "";
    public string EngineVersion { get; private set; } = "";
    public decimal NozzleDiameterMm { get; private init; }
    public decimal FilamentDiameterMm { get; private init; }
    public int ExtruderCount { get; private init; }
    public int MaximumColorSlots { get; private init; }
    public decimal WidthMm { get; private init; }
    public decimal DepthMm { get; private init; }
    public decimal HeightMm { get; private init; }
    public bool HeatedBed { get; private init; }
    public decimal RequiredNozzleTemperatureC { get; private init; }
    public decimal RequiredBedTemperatureC { get; private init; }
    public string SettingsJson { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public bool IsVerified { get; private set; }
    public string CuraDefinitionId { get; private set; } = "";
    public string CuraExtruderDefinitionId { get; private set; } = "";
    public string CuraVariantId { get; private set; } = "";
    public string CuraQualityId { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; }

    private CuraProfile() { }

    public static CuraProfile Import(string name, string manufacturer, string model, string material,
        string version, string engineVersion, decimal nozzle, decimal filament, int extruders, int colors,
        decimal width, decimal depth, decimal height, bool heatedBed, decimal nozzleTemperature,
        decimal bedTemperature, string settingsJson) => new()
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Manufacturer = manufacturer.Trim().ToUpperInvariant(),
            MachineModel = model.Trim().ToUpperInvariant(), MaterialCode = material.Trim().ToUpperInvariant(),
            Version = version.Trim(), EngineVersion = engineVersion.Trim(), NozzleDiameterMm = nozzle,
            FilamentDiameterMm = filament, ExtruderCount = extruders, MaximumColorSlots = colors,
            WidthMm = width, DepthMm = depth, HeightMm = height, HeatedBed = heatedBed,
            RequiredNozzleTemperatureC = nozzleTemperature, RequiredBedTemperatureC = bedTemperature,
            SettingsJson = settingsJson, CreatedAtUtc = DateTime.UtcNow
        };

    public void MarkVerified(string definitionId, string extruderDefinitionId, string variantId, string qualityId)
    {
        if (new[] { definitionId, extruderDefinitionId, qualityId }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Profile được xác minh phải có đủ definition, extruder và quality ID.");
        IsVerified = true;
        CuraDefinitionId = definitionId;
        CuraExtruderDefinitionId = extruderDefinitionId;
        CuraVariantId = variantId;
        CuraQualityId = qualityId;
    }

    public void Deactivate() => IsActive = false;

    public bool Matches(Machine m) => IsActive && m is { Technology: Enums.PrintTechnology.Fdm, PrinterConfiguration: { } c } &&
                                      (CuraDefinitionId.Length == 0 || CuraDefinitionId.Equals(c.CuraDefinitionId ?? "", StringComparison.OrdinalIgnoreCase)) &&
                                      Manufacturer.Equals(c.Manufacturer.Trim(), StringComparison.InvariantCultureIgnoreCase) && MachineModel.Equals(m.Model.Trim(), StringComparison.InvariantCultureIgnoreCase) &&
                                      NozzleDiameterMm == c.NozzleDiameterMm && FilamentDiameterMm == c.FilamentDiameterMm &&
                                      ExtruderCount == c.ExtruderCount && MaximumColorSlots == c.MaximumColorSlots &&
                                      WidthMm == m.BuildVolume.WidthMm && DepthMm == m.BuildVolume.DepthMm && HeightMm == m.BuildVolume.HeightMm &&
                                      HeatedBed == c.HeatedBed && RequiredNozzleTemperatureC <= c.MaxNozzleTemperatureC &&
                                      RequiredBedTemperatureC <= c.MaxBedTemperatureC && m.SupportedMaterials.Contains(MaterialCode);
}
