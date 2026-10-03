using System.Buffers.Binary;
using System.Text;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

namespace PrintGrid.UnitTests.Scheduling;

public class SlicingServiceTests
{
    private readonly PrintSlicingService _slicing = new();

    private const double H = 5.0; // half side of a 10 mm cube
    private const decimal ExpectedVolumeCm3 = 1.0m;

    [Fact]
    public async Task AnalyzeAsync_binary_stl_cube_returns_bbox_and_volume()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(WriteBinaryCube()), "STL");

        geometry.IsValid.Should().BeTrue();
        geometry.WidthMm.Should().BeApproximately(10m, 1e-4m);
        geometry.DepthMm.Should().BeApproximately(10m, 1e-4m);
        geometry.HeightMm.Should().BeApproximately(10m, 1e-4m);
        geometry.VolumeCm3.Should().BeApproximately(ExpectedVolumeCm3, 1e-4m);
        geometry.FaceCount.Should().Be(12);
    }

    [Fact]
    public async Task AnalyzeAsync_ascii_stl_cube_returns_bbox_and_volume()
    {
        var geometry = await _slicing.AnalyzeAsync(
            new MemoryStream(Encoding.ASCII.GetBytes(WriteAsciiCube())), "STL");

        geometry.IsValid.Should().BeTrue();
        geometry.VolumeCm3.Should().BeApproximately(ExpectedVolumeCm3, 1e-4m);
        geometry.HeightMm.Should().BeApproximately(10m, 1e-4m);
    }

    [Fact]
    public async Task AnalyzeAsync_obj_cube_returns_bbox_and_volume()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(Encoding.ASCII.GetBytes(WriteObjCube())), "OBJ");

        geometry.IsValid.Should().BeTrue();
        geometry.WidthMm.Should().BeApproximately(10m, 1e-4m);
        geometry.VolumeCm3.Should().BeApproximately(ExpectedVolumeCm3, 1e-4m);
        geometry.VertexCount.Should().Be(8);
        geometry.FaceCount.Should().Be(12);
    }

    [Fact]
    public async Task AnalyzeAsync_unsupported_format_returns_failed()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(new byte[16]), "3MF");

        geometry.IsValid.Should().BeFalse();
        geometry.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Estimate_computes_minutes_and_grams_for_reference_config()
    {
        var cube = await _slicing.AnalyzeAsync(new MemoryStream(WriteBinaryCube()), "STL");

        var estimate = _slicing.Estimate(cube, "PLA", 0.2m, 30, PrintTechnology.Fdm);

        // PLA: 1.24 g/cm³, infill 30% → 0.33 fill factor → 0.33 cm³ → ~0.41 g
        estimate.MaterialGrams.Should().BeApproximately(0.41m, 0.05m);
        estimate.PrintMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Estimate_material_density_changes_grams_but_not_minutes()
    {
        var geometry = new GeometryAnalysisPayload(30, 40);
        var pla = _slicing.Estimate(geometry.Geometry, "PLA", 0.2m, 30, PrintTechnology.Fdm);
        var abs = _slicing.Estimate(geometry.Geometry, "ABS", 0.2m, 30, PrintTechnology.Fdm);

        abs.MaterialGrams.Should().BeLessThan(pla.MaterialGrams);
        abs.PrintMinutes.Should().Be(pla.PrintMinutes);
    }

    [Fact]
    public async Task Estimate_higher_resolution_prints_longer()
    {
        var cube = await _slicing.AnalyzeAsync(new MemoryStream(WriteBinaryCube()), "STL");

        var draft = _slicing.Estimate(cube, "PLA", 0.3m, 30, PrintTechnology.Fdm);
        var ultra = _slicing.Estimate(cube, "PLA", 0.05m, 30, PrintTechnology.Fdm);

        ultra.PrintMinutes.Should().BeGreaterThan(draft.PrintMinutes);
    }

    [Fact]
    public void EstimateFromMetadata_is_deterministic_and_scales_with_file_size()
    {
        var small = _slicing.EstimateFromMetadata(200_000, "PLA", 0.2m, 30, PrintTechnology.Fdm);
        var large = _slicing.EstimateFromMetadata(2_000_000, "PLA", 0.2m, 30, PrintTechnology.Fdm);

        small.PrintMinutes.Should().Be(_slicing.EstimateFromMetadata(200_000, "PLA", 0.2m, 30, PrintTechnology.Fdm).PrintMinutes);
        large.PrintMinutes.Should().BeGreaterThan(small.PrintMinutes);
        large.MaterialGrams.Should().BeGreaterThan(small.MaterialGrams);
    }

    /// <summary>Convenience geometry: a 30×30×40 mm box, 36 cm³.</summary>
    private sealed record GeometryAnalysisPayload(decimal SideMm, decimal HeightMm)
    {
        public GeometryAnalysis Geometry { get; } = new(
            IsValid: true,
            WidthMm: SideMm,
            DepthMm: SideMm,
            HeightMm: HeightMm,
            VolumeCm3: SideMm * SideMm * HeightMm / 1000m,
            VertexCount: 0,
            FaceCount: 0);
    }

    // ── binary STL cube (10 mm) ────────────────────────────────────────

    public static byte[] WriteBinaryCube()
    {
        var triangles = CubeTriangles();
        var bytes = new byte[80 + 4 + triangles.Count * 50];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80, 4), (uint)triangles.Count);

        var offset = 84;
        foreach (var tri in triangles)
        {
            WriteFloat(bytes, offset, 0f); WriteFloat(bytes, offset + 4, 0f); WriteFloat(bytes, offset + 8, 1f);
            offset += 12;
            foreach (var (x, y, z) in tri)
            {
                WriteFloat(bytes, offset, (float)x);
                WriteFloat(bytes, offset + 4, (float)y);
                WriteFloat(bytes, offset + 8, (float)z);
                offset += 12;
            }
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), 0);
            offset += 2;
        }
        return bytes;
    }

    private static void WriteFloat(Span<byte> buffer, int offset, float value) =>
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], BitConverter.SingleToInt32Bits(value));

    private static string WriteAsciiCube()
    {
        var sb = new StringBuilder("solid cube\n");
        foreach (var tri in CubeTriangles())
        {
            sb.Append("  facet normal 0 0 1\n    outer loop\n");
            foreach (var (x, y, z) in tri)
            {
                sb.Append($"      vertex {x:F6} {y:F6} {z:F6}\n");
            }
            sb.Append("    endloop\n  endfacet\n");
        }
        return sb.Append("endsolid cube\n").ToString();
    }

    private static string WriteObjCube()
    {
        var vertices = new[]
        {
            (-H, -H, -H), (H, -H, -H), (H, H, -H), (-H, H, -H),
            (-H, -H, H), (H, -H, H), (H, H, H), (-H, H, H),
        };

        // 6 faces × 2 triangles = 12, clockwise-wound OBJ faces.
        var cubeFaces = new[]
        {
            (1, 3, 4), (1, 2, 3), // bottom
            (5, 7, 8), (5, 6, 7), // top
            (2, 6, 7), (2, 3, 7), // +X
            (1, 5, 8), (1, 4, 8), // -X
            (3, 7, 8), (3, 4, 8), // +Y
            (1, 5, 6), (1, 2, 6), // -Y
        };

        var sb = new StringBuilder("# cube\n");
        foreach (var (x, y, z) in vertices) sb.Append($"v {x:F6} {y:F6} {z:F6}\n");
        foreach (var (a, b, c) in cubeFaces) sb.Append($"f {a} {b} {c}\n");
        return sb.ToString();
    }

    private static List<(double X, double Y, double Z)[]> CubeTriangles()
    {
        var tri = new List<(double, double, double)[]>
        {
            // bottom (+Z is up in these tests; convention below keeps a closed signed volume)
            new[] { (-H, -H, -H), (-H, H, -H), (H, H, -H) },
            new[] { (-H, -H, -H), (H, H, -H), (H, -H, -H) },
            // top
            new[] { (-H, -H, H), (H, -H, H), (H, H, H) },
            new[] { (-H, -H, H), (H, H, H), (-H, H, H) },
            // +X
            new[] { (H, -H, -H), (H, H, -H), (H, H, H) },
            new[] { (H, -H, -H), (H, H, H), (H, -H, H) },
            // -X
            new[] { (-H, -H, -H), (-H, H, H), (-H, -H, H) },
            new[] { (-H, -H, -H), (-H, H, -H), (-H, H, H) },
            // +Y
            new[] { (-H, H, -H), (H, H, -H), (H, H, H) },
            new[] { (-H, H, -H), (H, H, H), (-H, H, H) },
            // -Y
            new[] { (-H, -H, -H), (-H, -H, H), (H, -H, H) },
            new[] { (-H, -H, -H), (H, -H, H), (H, -H, -H) },
        };
        return tri;
    }
}