using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.ValueObjects;

namespace PrintGrid.UnitTests.Scheduling;

internal static class MachineBuilder
{
    internal static Machine Default(
        Guid? labId = null,
        BuildVolume? buildVolume = null,
        decimal minLayerHeight = 0.1m,
        decimal achievableTolerance = 0.2m,
        IEnumerable<string>? materials = null,
        PrintTechnology technology = PrintTechnology.Fdm)
    {
        var machine = Machine.Register(
            labId ?? Guid.NewGuid(),
            "Printer-01",
            "Prusa MK4",
            technology,
            buildVolume ?? BuildVolume.Create(250, 210, 220),
            minLayerHeight,
            achievableTolerance,
            materials ?? ["PLA", "PETG"]);
        machine.ConfigurePrinter(new PrinterConfiguration("Prusa", 1, 1, .4m, 1.75m, true, 300, 120, 6000, .08m));
        machine.SetMatchedProfiles([Guid.NewGuid()], true);
        machine.ReviewProfile(true, Guid.NewGuid(), null);
        return machine;
    }
}

internal static class SpecBuilder
{
    internal static JobSpecification Default(
        BuildVolume? volume = null,
        string material = "PLA",
        string color = "BLACK",
        decimal layerHeight = 0.2m,
        decimal tolerance = 0.3m,
        PrintTechnology technology = PrintTechnology.Fdm,
        decimal materialGrams = 45m) =>
        JobSpecification.Create(
            volume ?? BuildVolume.Create(80, 80, 80),
            material,
            color,
            layerHeight,
            tolerance,
            technology,
            materialGrams);
}
