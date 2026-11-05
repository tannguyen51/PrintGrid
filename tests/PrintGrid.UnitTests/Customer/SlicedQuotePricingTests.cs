using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.UnitTests.Customer;

public class SlicedQuotePricingTests
{
    [Fact]
    public void Resolved_extruder_settings_follow_customer_layer_and_infill_overrides()
    {
        var stack = System.Text.Json.Nodes.JsonNode.Parse("""
            {"layer_height":0.2,"infill_sparse_density":20,"infill_line_distance":4,
             "infill_sparse_thickness":0.2,"top_thickness":1,"bottom_thickness":1,
             "top_layers":5,"bottom_layers":5}
            """)!.AsObject();
        CuraQuoteSlicer.ApplyQuoteSettings(stack, .1m, 80);
        stack["layer_height"]!.GetValue<decimal>().Should().Be(.1m);
        stack["infill_sparse_density"]!.GetValue<int>().Should().Be(80);
        stack["infill_line_distance"]!.GetValue<decimal>().Should().Be(1);
        stack["infill_sparse_thickness"]!.GetValue<decimal>().Should().Be(.1m);
        stack["top_layers"]!.GetValue<int>().Should().Be(10);
    }

    private static PricingParameterSet Rates => PricingParameterSet.Active with
    {
        PricePerPrintHour = 6000,
        FailureRate = .08m,
        PolishingPerUnit = 30000,
        ColorPricePerKg = new Dictionary<string, decimal>
        { ["PLA:BLUE"] = 300000, ["PLA:WHITE"] = 290000, ["PLA:SIGNALRED"] = 310000 }
    };

    [Fact]
    public void Three_color_batch_matches_agreed_formula_without_any_platform_fees()
    {
        var slice = new QuoteSliceResult([
            new QuoteColorUsage("BLUE", 460),
            new QuoteColorUsage("WHITE", 460), new QuoteColorUsage("SIGNALRED", 460)
        ], 65 * 60);
        var price = SlicedQuotePricing.Calculate(Rates, "PLA", 5, slice, true);
        price.MaterialAmount.Should().Be(414000);
        price.MachineAmount.Should().Be(390000);
        price.ServiceAmount.Should().Be(150000);
        price.FailureAllowance.Should().BeApproximately(69913.043478m, .0001m);
        price.TotalAmount.Should().Be(1023913);
    }

    [Theory]
    [InlineData("S", .625)]
    [InlineData("M", 1.25)]
    [InlineData("L", 1.875)]
    [InlineData("ORIGINAL", 1)]
    public void Size_changes_all_dimensions_by_same_factor(string size, decimal expected)
        => SlicedQuotePricing.ScaleFor(size, 120, 80, 160).Should().Be(expected);

    [Fact]
    public void No_polishing_and_zero_failure_add_no_extra_charge()
    {
        var rates = Rates with { FailureRate = 0 };
        var price = SlicedQuotePricing.Calculate(rates, "PLA", 1,
            new QuoteSliceResult([new QuoteColorUsage("BLUE", 100)], 60), false);
        price.TotalAmount.Should().Be(36000);
        price.ServiceAmount.Should().Be(0);
    }

    [Fact]
    public void Machine_contract_overrides_hourly_rate_and_failure_only()
    {
        var price = SlicedQuotePricing.Calculate(Rates, "PLA", 1,
            new QuoteSliceResult([new QuoteColorUsage("BLUE", 100)], 60, 10000, 0), false);
        price.TotalAmount.Should().Be(40000);
    }

    [Fact]
    public void Hundred_percent_failure_is_rejected()
    {
        var act = () => SlicedQuotePricing.Calculate(Rates with { FailureRate = 1 }, "PLA", 1,
            new QuoteSliceResult([new QuoteColorUsage("BLUE", 100)], 60), false);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cura_mm3_header_converts_to_grams_and_multiplies_quantity_once()
    {
        var result = CuraQuoteSlicer.ParseGcodeHeader([";TIME:3600", ";Filament used: 1000mm3, 2000mm3, 3000mm3"],
            ["BLUE", "WHITE", "SIGNALRED"], 1.24m, 5);
        result.PrintMinutes.Should().Be(300);
        result.Colors.Select(c => c.Grams).Should().Equal(6.2m, 12.4m, 18.6m);
    }

    [Fact]
    public void Linear_filament_metres_use_diameter_not_density_alone()
    {
        var result = CuraQuoteSlicer.ParseGcodeHeader([";TIME:60", ";Filament used: 1m"],
            ["BLUE"], 1.24m, 1, [1.75m]);
        result.Colors.Single().Grams.Should().BeApproximately(2.9825m, .0001m);
    }

    [Fact]
    public void Missing_or_wrong_count_material_output_fails_instead_of_inventing_equal_color_weights()
    {
        var act = () => CuraQuoteSlicer.ParseGcodeHeader([";TIME:60", ";Filament used: 1000mm3"],
            ["BLUE", "WHITE"], 1.24m, 1);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Griffin_header_supports_more_than_two_extruders()
    {
        var result = CuraQuoteSlicer.ParseGcodeHeader([
            ";PRINT.TIME:120",
            ";EXTRUDER_TRAIN.0.MATERIAL.VOLUME_USED:1000", ";EXTRUDER_TRAIN.1.MATERIAL.VOLUME_USED:2000",
            ";EXTRUDER_TRAIN.2.MATERIAL.VOLUME_USED:3000"
        ], ["BLUE", "WHITE", "SIGNALRED"], 1m, 1);
        result.PrintMinutes.Should().Be(2);
        result.Colors.Select(c => c.Grams).Should().Equal(1m, 2m, 3m);
    }

    [Fact]
    public void Multi_color_material_reservations_release_and_settle_all_colors()
    {
        var lab = Lab.Onboard("Lab", "HCM", 1);
        lab.SetMaterialStock("PLA", "BLUE", 100);
        lab.SetMaterialStock("PLA", "WHITE", 100);
        var job = Guid.NewGuid();
        lab.ReserveMaterial(job, "PLA", "BLUE", 10);
        lab.ReserveMaterial(job, "PLA", "WHITE", 20);
        lab.ReserveMaterial(job, "PLA", "BLUE", 10);
        lab.MaterialReservations.Should().HaveCount(2);
        lab.SettleMaterial(job, 30);
        lab.MaterialReservations.Should().BeEmpty();
        lab.MaterialStocks.Single(s => s.ColorCode == "BLUE").AvailableGrams.Should().Be(90);
        lab.MaterialStocks.Single(s => s.ColorCode == "WHITE").AvailableGrams.Should().Be(80);
    }

    [Fact]
    public async Task Three_mf_reads_actual_build_transforms_and_unit_instead_of_unused_resource_vertices()
    {
        using var content = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(content, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            await using var writer = new StreamWriter(archive.CreateEntry("3D/3dmodel.model").Open());
            await writer.WriteAsync("""
                                    <model xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02" unit="centimeter">
                                      <resources>
                                        <object id="1" type="model"><mesh>
                                          <vertices><vertex x="0" y="0" z="0"/><vertex x="1" y="0" z="0"/><vertex x="0" y="1" z="0"/><vertex x="0" y="0" z="1"/></vertices>
                                          <triangles><triangle v1="0" v2="2" v3="1"/><triangle v1="0" v2="1" v3="3"/><triangle v1="0" v2="3" v3="2"/><triangle v1="1" v2="2" v3="3"/></triangles>
                                        </mesh></object>
                                        <object id="2" type="model"><components><component objectid="1" transform="1 0 0 0 1 0 0 0 1 3 0 0"/></components></object>
                                      </resources><build><item objectid="1"/><item objectid="2"/></build>
                                    </model>
                                    """);
        }
        content.Position = 0;
        var analysis = await new PrintGrid.Modules.Scheduling.Infrastructure.Slicing.PrintSlicingService().AnalyzeAsync(content, "3MF");
        analysis.IsValid.Should().BeTrue();
        analysis.WidthMm.Should().Be(40);
        analysis.DepthMm.Should().Be(10);
        analysis.HeightMm.Should().Be(10);
        analysis.VolumeCm3.Should().BeApproximately(.333m, .001m);
        analysis.IsWatertight.Should().BeTrue();
    }
}
