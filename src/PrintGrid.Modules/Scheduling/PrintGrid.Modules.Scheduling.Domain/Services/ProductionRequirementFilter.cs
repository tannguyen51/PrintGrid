using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Scheduling.Domain.Services;

public static class ProductionRequirementFilter
{
    public static string? Reject(Machine machine, ProductionRequirements? requirements)
    {
        if (requirements is null) return null;
        var config = machine.PrinterConfiguration;
        if (config is null) return "machine_configuration_missing";
        if (requirements.PinnedModelId.HasValue &&
            MachineModelIdentity.For(config.Manufacturer, machine.Model) != requirements.PinnedModelId)
            return "pinned_model_mismatch";
        if (requirements.MaxColors is { } colors && config.MaximumColorSlots < colors) return "insufficient_color_slots";
        if (requirements.EnclosedChamber == true && !config.EnclosedChamber) return "enclosed_chamber_required";
        if (requirements.NozzleDiameterMm is { } nozzle && Math.Abs(config.NozzleDiameterMm - nozzle) > .001m)
            return "nozzle_diameter_mismatch";
        if (requirements.MaxToleranceMm is { } tolerance && machine.AchievableToleranceMm > tolerance)
            return "technical_tolerance_unachievable";
        if (requirements.MinLayerHeightMm is { } layer && machine.MinLayerHeightMm > layer)
            return "technical_layer_height_unachievable";
        return null;
    }
}
