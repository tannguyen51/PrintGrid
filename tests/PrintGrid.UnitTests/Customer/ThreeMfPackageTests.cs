using System.IO.Compression;
using System.Numerics;
using System.Text;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Customer;

public sealed class ThreeMfPackageTests
{
    private const string Ns = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    private const string Production = "http://schemas.microsoft.com/3dmanufacturing/production/2015/06";
    private const string Vertices = "<vertices><vertex x='0' y='0' z='0'/><vertex x='2' y='0' z='0'/><vertex x='0' y='2' z='0'/><vertex x='0' y='0' z='2'/></vertices>";

    [Fact]
    public void Package_paths_preserve_instances_units_and_transforms()
    {
        using var zip = Package(("3D/3dmodel.model", $"""
            <model xmlns="{Ns}" xmlns:p="{Production}" unit="centimeter"><resources>
              <object id="1"><components><component objectid="1" p:path="Objects/part.model" transform="1 0 0 0 1 0 0 0 1 3 0 0"/></components></object>
            </resources><build><item objectid="1"/><item objectid="1" transform="1 0 0 0 1 0 0 0 1 4 0 0"/></build></model>
            """), ("3D/Objects/part.model", Model("<triangle v1='0' v2='1' v3='2'/>", "millimeter")));
        var groups = ThreeMfPackage.Read(zip, false);
        groups.Should().HaveCount(2);
        groups[0][0].A.X.Should().BeApproximately(30, .0001f);
        groups[0][0].B.X.Should().BeApproximately(32, .0001f);
        groups[1][0].A.X.Should().BeApproximately(70, .0001f);
    }

    [Fact]
    public void Object_and_part_filament_assignments_override_inherited_slot()
    {
        using var zip = Package(("3D/3dmodel.model", $"""
            <model xmlns="{Ns}"><resources>
              <object id="1"><components><component objectid="2"/><component objectid="3"/></components></object>
              <object id="2"><mesh>{Vertices}<triangles><triangle v1="0" v2="1" v3="2"/></triangles></mesh></object>
              <object id="3"><mesh>{Vertices}<triangles><triangle v1="0" v2="1" v3="3"/></triangles></mesh></object>
            </resources><build><item objectid="1"/></build></model>
            """), ("Metadata/project_settings.config", "{\"filament_colour\":[\"#FFFFFF\",\"#FF0000\"]}"),
            ("Metadata/model_settings.config", "<config><object id='1'><metadata key='extruder' value='2'/><part id='3'><metadata key='extruder' value='1'/></part></object></config>"));
        var groups = QuoteMesh.Read(zip, "3MF");
        groups.Should().HaveCount(2);
        groups[0].Single().C.Should().Be(new Vector3(0, 0, 2));
        groups[1].Single().C.Should().Be(new Vector3(0, 2, 0));
    }

    [Theory]
    [InlineData("4", 1)]
    [InlineData("8", 2)]
    [InlineData("0C", 3)]
    [InlineData("EC", 17)]
    [InlineData("0FC", 18)]
    public void Paint_leaf_codes_recover_filament_slots(string code, int slot)
    {
        var result = new List<int>();
        ThreeMfPaint.Decode(code, new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY), 1, (_, s) => result.Add(s));
        result.Should().Equal(slot);
    }

    [Fact]
    public void Split_paint_keeps_child_order_midpoints_and_winding()
    {
        var result = new List<(QuoteMesh.Triangle Triangle, int Slot)>();
        ThreeMfPaint.Decode("481", new(Vector3.Zero, new(2, 0, 0), new(0, 2, 0)), 1, (t, s) => result.Add((t, s)));
        result.Select(r => r.Slot).Should().Equal(2, 1);
        result[0].Triangle.Should().Be(new QuoteMesh.Triangle(new(1, 1, 0), new(0, 2, 0), Vector3.Zero));
        result.Sum(r => Vector3.Cross(r.Triangle.B - r.Triangle.A, r.Triangle.C - r.Triangle.A).Z / 2).Should().Be(2);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("C")]
    [InlineData("44")]
    [InlineData("Z")]
    [InlineData("F")]
    public void Malformed_paint_is_rejected(string code)
    {
        var action = () => ThreeMfPaint.Decode(code, new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY), 1, (_, _) => { });
        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public async Task Color_boundaries_do_not_make_a_closed_mesh_non_watertight()
    {
        using var zip = Package(("3D/3dmodel.model", Model("""
            <triangle v1='0' v2='2' v3='1' paint_color='4'/><triangle v1='0' v2='1' v3='3' paint_color='8'/>
            <triangle v1='0' v2='3' v3='2' paint_color='4'/><triangle v1='1' v2='2' v3='3' paint_color='8'/>
            """)));
        var analysis = await new PrintSlicingService().AnalyzeAsync(zip, "3MF");
        analysis.IsValid.Should().BeTrue();
        analysis.IsWatertight.Should().BeTrue();
        analysis.IsManifold.Should().BeTrue();
        analysis.VolumeCm3.Should().BeApproximately(.001m, .001m);
    }

    [Theory]
    [InlineData("../../part.model")]
    [InlineData("https://example.com/model.model")]
    [InlineData("/missing.model")]
    public void Missing_and_external_references_fail_explicitly(string path)
    {
        using var zip = Package(("3D/3dmodel.model", $"<model xmlns='{Ns}' xmlns:p='{Production}'><resources><object id='1'><components><component objectid='1' p:path='{path}'/></components></object></resources><build><item objectid='1'/></build></model>"));
        var action = () => ThreeMfPackage.Read(zip);
        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Surface_colors_cannot_be_quoted_as_separate_solid_volumes()
    {
        using var zip = Package(("3D/3dmodel.model", Model("<triangle v1='0' v2='1' v3='2' paint_color='4'/>")));
        var action = () => QuoteMesh.Read(zip, "3MF", allowSurfacePainting: false);
        action.Should().Throw<InvalidDataException>().WithMessage("*slicer*");
    }

    [Fact]
    public void Component_cycles_are_rejected()
    {
        using var zip = Package(("3D/3dmodel.model", $"<model xmlns='{Ns}'><resources><object id='1'><components><component objectid='1'/></components></object></resources><build><item objectid='1'/></build></model>"));
        var action = () => ThreeMfPackage.Read(zip);
        action.Should().Throw<InvalidDataException>().WithMessage("*vòng lặp*");
    }

    [Fact]
    public void Expanded_mesh_over_old_100_MB_limit_is_streamed()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        using (var writer = new StreamWriter(zip.CreateEntry("3D/3dmodel.model").Open()))
        {
            writer.Write($"<model xmlns='{Ns}'><resources><object id='1'><mesh>{Vertices}<triangles><triangle v1='0' v2='1' v3='2'/></triangles></mesh></object></resources>");
            var padding = new string(' ', 1024 * 1024);
            for (var i = 0; i < 101; i++) writer.Write(padding);
            writer.Write("<build><item objectid='1'/></build></model>");
        }
        stream.Position = 0;
        ThreeMfPackage.Read(stream).Single().Should().ContainSingle();
    }

    private static string Model(string faces, string unit = "millimeter") => $"<model xmlns='{Ns}' unit='{unit}'><resources><object id='1'><mesh>{Vertices}<triangles>{faces}</triangles></mesh></object></resources><build><item objectid='1'/></build></model>";
    private static MemoryStream Package(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var file in files)
            {
                using var writer = new StreamWriter(zip.CreateEntry(file.Name).Open(), Encoding.UTF8);
                writer.Write(file.Content);
            }
        stream.Position = 0;
        return stream;
    }
}
