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

    public bool Matches(Machine m) => IsActive && m is { Technology: Enums.PrintTechnology.Fdm, PrinterConfiguration: { } c } &&
                                      Manufacturer.Equals(c.Manufacturer.Trim(), StringComparison.InvariantCultureIgnoreCase) && MachineModel.Equals(m.Model.Trim(), StringComparison.InvariantCultureIgnoreCase) &&
                                      NozzleDiameterMm == c.NozzleDiameterMm && FilamentDiameterMm == c.FilamentDiameterMm &&
                                      ExtruderCount == c.ExtruderCount && MaximumColorSlots == c.MaximumColorSlots &&
                                      WidthMm == m.BuildVolume.WidthMm && DepthMm == m.BuildVolume.DepthMm && HeightMm == m.BuildVolume.HeightMm &&
                                      HeatedBed == c.HeatedBed && RequiredNozzleTemperatureC <= c.MaxNozzleTemperatureC &&
                                      RequiredBedTemperatureC <= c.MaxBedTemperatureC && m.SupportedMaterials.Contains(MaterialCode);
}
