using System.Buffers.Binary;
using System.Text;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

/// <summary>
/// Parses binary and ASCII STL into a mesh: bounding box (mm), enclosed volume (cm³) via
/// signed tetrahedra, and face/vertex counts. Pure C#, no third-party geometry dependency.
/// </summary>
internal static class StlMeshParser
{
    private const int BinaryHeaderSize = 80;
    private const int TriangleRecordSize = 50; // normal(12) + 3 verts(36) + attr(2)

    public static MeshStats Parse(Stream stream)
    {
        // Peek: a binary STL starts with 80 header bytes then the triangle count as uint32.
        // An ASCII STL starts with "solid" followed by whitespace then usually "facet" or a name.
        using var buffered = new BufferedStream(stream, 64 * 1024);
        Span<byte> lead = stackalloc byte[5];
        ReadExact(buffered, lead);
        buffered.Position = 0;

        if (lead is [0x73, 0x6F, 0x6C, 0x69, 0x64]) // "solid"
        {
            // Ambiguous: binary files may also begin with "solid". After the 5 bytes, if we
            // can parse an ASCII body within it we treat as ASCII; otherwise fall back to binary.
            var ascii = TryParseAscii(buffered);
            if (ascii is not null) return ascii;

            buffered.Position = 0;
            return ParseBinary(buffered);
        }

        return ParseBinary(buffered);
    }

    private static MeshStats ParseBinary(Stream stream)
    {
        Span<byte> header = stackalloc byte[BinaryHeaderSize];
        ReadExact(stream, header);

        Span<byte> countBytes = stackalloc byte[4];
        ReadExact(stream, countBytes);
        var triangleCount = BinaryPrimitives.ReadUInt32LittleEndian(countBytes);
        if (triangleCount is 0 or > 10_000_000)
            throw new InvalidDataException($"Suspicious triangle count {triangleCount}");

        Span<byte> record = stackalloc byte[TriangleRecordSize];
        var min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
        var max = new double[] { double.MinValue, double.MinValue, double.MinValue };
        double signedVolume = 0, absVolume = 0;
        var faceCount = 0;

        for (var i = 0; i < triangleCount; i++)
        {
            ReadExact(stream, record);

            // Skip normal (12 bytes), read 3 vertices of 3 doubles little-endian.
            var v1 = ReadVector(record, 12);
            var v2 = ReadVector(record, 24);
            var v3 = ReadVector(record, 36);

            UpdateBounds(min, max, v1, v2, v3);
            var tri = SignedTetraVolume(v1, v2, v3);
            signedVolume += tri;
            absVolume += Math.Abs(tri);
            faceCount++;
        }

        var closed = Math.Abs(signedVolume) > 1e-6;
        var volume = closed ? Math.Abs(signedVolume) : absVolume;

        return new MeshStats(
            WidthMm: max[0] - min[0],
            DepthMm: max[1] - min[1],
            HeightMm: max[2] - min[2],
            VolumeCm3: volume / 1000.0,
            VertexCount: faceCount * 3,
            FaceCount: faceCount);
    }

    private static MeshStats? TryParseAscii(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var facetCount = 0;
        var min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
        var max = new double[] { double.MinValue, double.MinValue, double.MinValue };
        double signedVolume = 0, absVolume = 0;

        string? line;
        var sawFacet = false;
        while ((line = reader.ReadLine()) is not null)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;

            switch (tokens[0])
            {
                case "facet":
                    sawFacet = true;
                    break;
                case "vertex":
                    if (tokens.Length >= 4 &&
                        double.TryParse(tokens[1], out var vx) &&
                        double.TryParse(tokens[2], out var vy) &&
                        double.TryParse(tokens[3], out var vz))
                    {
                        UpdatePoint(min, max, vx, vy, vz);
                    }
                    break;
                case "endfacet":
                    facetCount++;
                    break;
            }
        }

        if (!sawFacet) return null;

        // Re-walk collecting triples for the volume (vertices appear in groups of 3 per facet).
        stream.Position = 0;
        using var volumeReader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var verts = new List<(double X, double Y, double Z)>(3);
        string? vline;
        while ((vline = volumeReader.ReadLine()) is not null)
        {
            var tokens = vline.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || tokens[0] != "vertex" || tokens.Length < 4) continue;

            if (!double.TryParse(tokens[1], out var vx) ||
                !double.TryParse(tokens[2], out var vy) ||
                !double.TryParse(tokens[3], out var vz)) continue;

            verts.Add((vx, vy, vz));
            if (verts.Count == 3)
            {
                var tri = SignedTetraVolume(verts[0], verts[1], verts[2]);
                signedVolume += tri;
                absVolume += Math.Abs(tri);
                verts.Clear();
            }
        }

        var closed = Math.Abs(signedVolume) > 1e-6;
        var volume = closed ? Math.Abs(signedVolume) : absVolume;

        return new MeshStats(
            WidthMm: max[0] == double.MinValue ? 0 : max[0] - min[0],
            DepthMm: max[1] == double.MinValue ? 0 : max[1] - min[1],
            HeightMm: max[2] == double.MinValue ? 0 : max[2] - min[2],
            VolumeCm3: volume / 1000.0,
            VertexCount: facetCount * 3,
            FaceCount: facetCount);
    }

    private static (double, double, double) ReadVector(ReadOnlySpan<byte> record, int offset)
    {
        // STL vertices are three float32s packed consecutively (12 bytes per vertex).
        var x = (double)BinaryPrimitives.ReadSingleLittleEndian(record.Slice(offset, 4));
        var y = (double)BinaryPrimitives.ReadSingleLittleEndian(record.Slice(offset + 4, 4));
        var z = (double)BinaryPrimitives.ReadSingleLittleEndian(record.Slice(offset + 8, 4));
        return (x, y, z);
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read <= 0) throw new EndOfStreamException("Unexpected end of STL stream");
            total += read;
        }
    }

    private static void UpdateBounds(double[] min, double[] max, params (double X, double Y, double Z)[] points)
    {
        foreach (var p in points) UpdatePoint(min, max, p.X, p.Y, p.Z);
    }

    private static void UpdatePoint(double[] min, double[] max, double x, double y, double z)
    {
        min[0] = Math.Min(min[0], x); max[0] = Math.Max(max[0], x);
        min[1] = Math.Min(min[1], y); max[1] = Math.Max(max[1], y);
        min[2] = Math.Min(min[2], z); max[2] = Math.Max(max[2], z);
    }

    /// <summary>Signed volume of the tetrahedron formed by the triangle and the origin (Newell-style).</summary>
    private static double SignedTetraVolume((double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c)
    {
        var cx = b.Y * c.Z - b.Z * c.Y;
        var cy = b.Z * c.X - b.X * c.Z;
        var cz = b.X * c.Y - b.Y * c.X;
        return (a.X * cx + a.Y * cy + a.Z * cz) / 6.0;
    }
}