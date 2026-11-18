using System.Text.Json.Nodes;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Customer;

public sealed class CuraBuildPlateTests
{
    [Theory]
    [InlineData("rectangular", 100, 100, 10, true)]
    [InlineData("elliptic", 100, 100, 10, false)]
    [InlineData("elliptic", 60, 80, 10, true)]
    [InlineData("elliptic", 61, 80, 10, false)]
    [InlineData("elliptic", 40, 40, 101, false)]
    [InlineData("rectangular", 101, 40, 10, false)]
    public void Model_must_fit_the_actual_bed_shape(string shape, decimal width, decimal depth,
        decimal height, bool fits)
    {
        var settings = new JsonObject
        {
            ["machine_width"] = 100, ["machine_depth"] = 100, ["machine_height"] = 100,
            ["machine_shape"] = shape
        };
        CuraQuoteSlicer.FitsBuildPlate(settings, width, depth, height).Should().Be(fits);
    }

    [Fact]
    public void Elliptical_bed_uses_both_axis_lengths()
    {
        var settings = new JsonObject
        {
            ["machine_width"] = 200, ["machine_depth"] = 100, ["machine_height"] = 100,
            ["machine_shape"] = "elliptic"
        };
        CuraQuoteSlicer.FitsBuildPlate(settings, 120, 80, 10).Should().BeTrue();
        CuraQuoteSlicer.FitsBuildPlate(settings, 160, 80, 10).Should().BeFalse();
    }
}
