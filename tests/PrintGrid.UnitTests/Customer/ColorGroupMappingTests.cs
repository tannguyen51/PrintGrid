using System.Numerics;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.UnitTests.Customer;

public sealed class ColorGroupMappingTests
{
    private static readonly QuoteMesh.Triangle First = new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);
    private static readonly QuoteMesh.Triangle Second = new(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ);
    private static IReadOnlyList<ThreeMfPackage.ColorGroup> Source => [
        new("filament:00000001", [First], "#FFFFFF"),
        new("filament:00000003", [Second], "#D02727")];

    [Fact]
    public void Stable_source_ids_determine_regions_even_when_request_order_changes()
    {
        var result = ColorGroupMapping.Apply(Source,
            [new("filament:00000003", "BLUE", "#FORGED"), new("filament:00000001", "WHITE")], ["BLUE", "WHITE"]);
        result.Groups[0].Should().Equal(Second);
        result.Groups[1].Should().Equal(First);
        result.Assignments.Should().Equal(new ColorGroupAssignment("filament:00000001", "WHITE", "#FFFFFF"),
            new ColorGroupAssignment("filament:00000003", "BLUE", "#D02727"));
    }

    [Fact]
    public void Several_source_regions_can_share_one_physical_color_without_losing_the_mapping()
    {
        var result = ColorGroupMapping.Apply(Source,
            [new("filament:00000001", "blue"), new("filament:00000003", "BLUE")], ["BLUE"]);
        result.Groups.Should().ContainSingle();
        result.Groups[0].Should().Equal(First, Second);
        result.Assignments.Should().HaveCount(2).And.OnlyContain(a => a.ColorCode == "BLUE");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("target")]
    public void Incomplete_forged_or_inconsistent_mappings_are_rejected(string mode)
    {
        List<ColorGroupAssignment> assignments = [new("filament:00000001", "BLUE"), new("filament:00000003", "WHITE")];
        if (mode == "missing") assignments.RemoveAt(1);
        if (mode == "duplicate") assignments[1] = assignments[0];
        if (mode == "unknown") assignments[1] = new("filament:00000002", "WHITE");
        var action = () => ColorGroupMapping.Apply(Source, assignments, mode == "target" ? ["GREEN"] : ["BLUE", "WHITE"]);
        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Machine_requirements_count_unique_target_colors_instead_of_source_regions()
    {
        var model = Model.Create(Guid.NewGuid(), "part", null, "part.3mf", "3MF", 1000, null, "models/part.3mf", "hash");
        model.ApplyGeometry(10, 10, 10, 1, 1, 1, true, true, true);
        var command = new CreateQuoteCommand(model.CustomerId, model.Id, "PLA", "WHITE", .2m, 30, 1, .2m,
            ColorAssignments: [new("filament:00000003", "blue"), new("filament:00000001", "BLUE")]);
        var criteria = MachineSelection.Criteria(command, model);
        criteria.Colors.Should().Equal("BLUE");
        new CreateQuoteCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_duplicate_source_ids_before_slicing()
    {
        var command = new CreateQuoteCommand(Guid.NewGuid(), Guid.NewGuid(), "PLA", "BLUE", .2m, 30, 1, .2m,
            ColorAssignments: [new("filament:00000001", "BLUE"), new("filament:00000001", "WHITE")]);
        new CreateQuoteCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }
}
