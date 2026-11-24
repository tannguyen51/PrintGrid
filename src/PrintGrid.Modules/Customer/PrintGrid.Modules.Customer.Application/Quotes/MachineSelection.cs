using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public static class MachineSelection
{
    public static MachineSelectionCriteria Criteria(CreateQuoteCommand command, Model model, QuoteModelEnvelope? envelope = null)
    {
        var width = envelope?.WidthMm ?? model.BoundingWidthMm ?? 0;
        var depth = envelope?.DepthMm ?? model.BoundingDepthMm ?? 0;
        var height = envelope?.HeightMm ?? model.BoundingHeightMm ?? 0;
        var scale = SlicedQuotePricing.ScaleFor(command.SizeCode, width, depth, height);
        var colors = command.ColorAssignments is { Count: > 0 }
            ? command.ColorAssignments.OrderBy(a => a.GroupId, StringComparer.Ordinal).Select(a => a.ColorCode.Trim().ToUpperInvariant()).Distinct().ToList()
            : command.ColorCodes ?? [command.ColorCode];
        return new(command.MaterialCode, colors,
            width * scale, depth * scale, height * scale, command.LayerHeightMm, command.ToleranceMm,
            TechConstraints.From(command.MaxColors, command.EnclosedChamber, command.NozzleDiameterMm,
                command.MaxToleranceMm, command.MinLayerHeightMm));
    }
}
