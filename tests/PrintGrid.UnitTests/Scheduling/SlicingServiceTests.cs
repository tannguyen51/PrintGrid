using System.Buffers.Binary;
using System.Text;
using System.IO.Compression;
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
        geometry.IsWatertight.Should().BeTrue();
        geometry.IsManifold.Should().BeTrue();
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
    public async Task AnalyzeAsync_valid_3mf_returns_mesh_statistics()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(WriteThreeMfTriangle()), "3MF");

        geometry.IsValid.Should().BeTrue();
        geometry.WidthMm.Should().Be(10m);
        geometry.HeightMm.Should().Be(0m);
        geometry.VertexCount.Should().Be(3);
        geometry.FaceCount.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_glb_cube_returns_bbox_and_volume()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(WriteGlbCube()), "GLB");

        geometry.IsValid.Should().BeTrue();
        geometry.WidthMm.Should().BeApproximately(10m, 1e-3m);
        geometry.DepthMm.Should().BeApproximately(10m, 1e-3m);
        geometry.HeightMm.Should().BeApproximately(10m, 1e-3m);
        geometry.VolumeCm3.Should().BeApproximately(ExpectedVolumeCm3, 1e-3m);
        geometry.VertexCount.Should().Be(8);
        geometry.FaceCount.Should().Be(12);
        geometry.IsWatertight.Should().BeTrue();
        geometry.IsManifold.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsync_glb_authored_in_metres_is_read_as_millimetres()
    {
        // A 10 mm part exported from a metre-based tool measures 0.01 units across;
        // the parser must scale it up rather than quote a 0.01 mm object.
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(WriteGlbCube(scale: 0.001)), "GLB");

        geometry.IsValid.Should().BeTrue();
        geometry.WidthMm.Should().BeApproximately(10m, 1e-3m);
        geometry.VolumeCm3.Should().BeApproximately(ExpectedVolumeCm3, 1e-3m);
    }

    [Fact]
    public async Task AnalyzeAsync_glb_without_container_magic_returns_failed()
    {
        var geometry = await _slicing.AnalyzeAsync(new MemoryStream(new byte[64]), "GLB");

        geometry.IsValid.Should().BeFalse();
        geometry.ErrorMessage.Should().Contain("glTF");
    }

    /// <summary>
    /// ModelFileInspector resets the stream's Position immediately after analysis, so a
    /// parser that disposed the caller's stream broke every upload (STL, OBJ and GLB alike)
    /// with ObjectDisposedException. The caller owns the stream — keep it open.
    /// </summary>
    [Theory]
    [InlineData("STL")]
    [InlineData("OBJ")]
    [InlineData("GLB")]
    public async Task AnalyzeAsync_leaves_the_callers_stream_open(string format)
    {
        using var stream = new MemoryStream(format switch
        {
            "STL" => WriteBinaryCube(),
            "OBJ" => Encoding.ASCII.GetBytes(WriteObjCube()),
            _ => WriteGlbCube(),
        });

        await _slicing.AnalyzeAsync(stream, format);

        stream.CanRead.Should().BeTrue();
        stream.Position = 0; // what ValidateContentAsync does next — threw before the fix
    }

    [Fact]
    public async Task AnalyzeAsync_open_mesh_reports_not_watertight()
    {
        const string triangle = "v 0 0 0\nv 10 0 0\nv 0 10 0\nf 1 2 3\n";

        var geometry = await _slicing.AnalyzeAsync(
            new MemoryStream(Encoding.ASCII.GetBytes(triangle)), "OBJ");

        geometry.IsValid.Should().BeTrue();
        geometry.IsWatertight.Should().BeFalse();
        geometry.IsManifold.Should().BeTrue();
        geometry.ErrorMessage.Should().Contain("not watertight");
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
                sb.Append(FormattableString.Invariant($"      vertex {x:F6} {y:F6} {z:F6}\n"));
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
        foreach (var (x, y, z) in vertices) sb.Append(FormattableString.Invariant($"v {x:F6} {y:F6} {z:F6}\n"));
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

    /// <summary>
    /// Minimal binary glTF (GLB) cube: 8 indexed vertices, 12 triangles, one BIN chunk.
    /// `scale` mimics the unit a tool exports in — 1.0 for millimetres, 0.001 for metres.
    /// </summary>
    private static byte[] WriteGlbCube(double scale = 1.0)
    {
        var vertices = new (double X, double Y, double Z)[]
        {
            (-H * scale, -H * scale, -H * scale), (H * scale, -H * scale, -H * scale),
            (H * scale, H * scale, -H * scale), (-H * scale, H * scale, -H * scale),
            (-H * scale, -H * scale, H * scale), (H * scale, -H * scale, H * scale),
            (H * scale, H * scale, H * scale), (-H * scale, H * scale, H * scale),
        };
        // Same winding as the OBJ fixture (1-based there, converted below).
        int[] faces =
        {
            1, 3, 4, 1, 2, 3, 5, 7, 8, 5, 6, 7, 2, 6, 7, 2, 3, 7,
            1, 5, 8, 1, 4, 8, 3, 7, 8, 3, 4, 8, 1, 5, 6, 1, 2, 6,
        };

        var positions = new byte[vertices.Length * 12];
        for (var i = 0; i < vertices.Length; i++)
        {
            WriteFloat(positions, i * 12, (float)vertices[i].X);
            WriteFloat(positions, i * 12 + 4, (float)vertices[i].Y);
            WriteFloat(positions, i * 12 + 8, (float)vertices[i].Z);
        }

        var indices = new byte[faces.Length * 2];
        for (var i = 0; i < faces.Length; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(indices.AsSpan(i * 2, 2), (ushort)(faces[i] - 1));

        var bin = positions.Concat(indices).ToArray();
        var json = FormattableString.Invariant($$"""
            {
              "asset": { "version": "2.0" },
              "scene": 0,
              "scenes": [ { "nodes": [ 0 ] } ],
              "nodes": [ { "mesh": 0 } ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "indices": 1, "mode": 4 } ] } ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": {{vertices.Length}}, "type": "VEC3",
                  "min": [ {{-H * scale}}, {{-H * scale}}, {{-H * scale}} ],
                  "max": [ {{H * scale}}, {{H * scale}}, {{H * scale}} ] },
                { "bufferView": 1, "componentType": 5123, "count": {{faces.Length}}, "type": "SCALAR" }
              ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": {{positions.Length}} },
                { "buffer": 0, "byteOffset": {{positions.Length}}, "byteLength": {{indices.Length}} }
              ],
              "buffers": [ { "byteLength": {{bin.Length}} } ]
            }
            """);

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var jsonPadded = (jsonBytes.Length + 3) / 4 * 4;
        var total = 12 + 8 + jsonPadded + 8 + bin.Length;
        var glb = new byte[total];

        var offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), 0x46546C67); offset += 4; // "glTF"
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), 2); offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), (uint)total); offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), (uint)jsonBytes.Length); offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), 0x4E4F534A); offset += 4; // "JSON"
        jsonBytes.CopyTo(glb.AsSpan(offset));
        for (var i = jsonBytes.Length; i < jsonPadded; i++) glb[offset + i] = 0x20; // pad with spaces
        offset += jsonPadded;

        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), (uint)bin.Length); offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(offset), 0x004E4942); offset += 4; // "BIN\0"
        bin.CopyTo(glb.AsSpan(offset));

        return glb;
    }

    private static byte[] WriteThreeMfTriangle()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <model unit="millimeter" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
              <resources><object id="1" type="model"><mesh>
                <vertices><vertex x="0" y="0" z="0"/><vertex x="10" y="0" z="0"/><vertex x="0" y="10" z="0"/></vertices>
                <triangles><triangle v1="0" v2="1" v3="2"/></triangles>
              </mesh></object></resources>
              <build><item objectid="1"/></build>
            </model>
            """;
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("3D/3dmodel.model");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(xml);
        }
        return output.ToArray();
    }
}
