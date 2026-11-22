using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public static class MachineSelection
{
    public static MachineSelectionCriteria Criteria(CreateQuoteCommand command, Model model)
    {
        var scale = SlicedQuotePricing.ScaleFor(command.SizeCode, model.BoundingWidthMm ?? 0,
            model.BoundingDepthMm ?? 0, model.BoundingHeightMm ?? 0);
        return new(command.MaterialCode, command.ColorCodes ?? [command.ColorCode],
            model.BoundingWidthMm!.Value * scale, model.BoundingDepthMm!.Value * scale,
            model.BoundingHeightMm!.Value * scale, command.LayerHeightMm, command.ToleranceMm,
            TechConstraints.From(command.MaxColors, command.EnclosedChamber, command.NozzleDiameterMm,
                command.MaxToleranceMm, command.MinLayerHeightMm));
    }
}
