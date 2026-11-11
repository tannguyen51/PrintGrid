using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Scheduling;

public sealed class CuraResourceCatalogTests
{
    private static CuraResourceCatalog Catalog()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["CuraLibrary:Version"] = "5.11.0" }).Build();
        return new(configuration);
    }

    [Fact]
    public void Loads_visible_machine_with_inherited_metadata_and_dimensions()
    {
        var machine = Catalog().Machines.Single(x => x.Id == "creality_ender3");
        machine.Manufacturer.Should().Be("Creality3D");
        machine.ExtruderDefinitionId.Should().Be("creality_base_extruder_0");
        machine.WidthMm.Should().Be(235);
        machine.HeightMm.Should().Be(250);
        machine.HeatedBed.Should().BeTrue();
        machine.ExtruderCount.Should().Be(1);
    }

    [Fact]
    public void Exposes_material_and_quality_ids_instead_of_display_names()
    {
        var options = Catalog().GetOptions("creality_ender3");
        options.Materials.Should().Contain(x => x.Id == "generic_pla_175" && x.MaterialCode == "PLA");
        options.Qualities.Should().Contain(x => x.Id == "base_0.4_PLA_standard");
        options.Qualities.Should().NotContain(x => x.Id.EndsWith(".inst", StringComparison.OrdinalIgnoreCase));
        options.Qualities.Should().NotContain(x => x.Id == "base_global_standard");
        options.Variants.Should().Contain(x => x.Id == "creality_base_0.4" && x.NozzleDiameterMm == 0.4m);
    }
}
