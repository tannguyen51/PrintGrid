using System.Globalization;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

/// <summary>
/// Minimal OBJ parser: vertices (v) and faces (f) with supports for negative indices,
/// vertex/texture/normal triplets (v/vt/vn), and polygons (triangulated via fan).
/// </summary>
internal static class ObjMeshParser
{
    public static MeshStats Parse(Stream stream)
    {
        var vertices = new List<(double X, double Y, double Z)>();
        var faces = new List<(int, int, int)>();

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0 || tokens[0].StartsWith('#')) continue;

            switch (tokens[0])
            {
                case "v" or "v " when tokens.Length >= 4:
                    if (TryParse(tokens, out var p)) vertices.Add(p);
                    break;

                case "f" when tokens.Length >= 4:
                    var indices = new List<int>(tokens.Length - 1);
                    for (var i = 1; i < tokens.Length; i++)
                    {
                        var idx = ResolveIndex(tokens[i].Split('/')[0], vertices.Count);
                        if (idx is not null) indices.Add(idx.Value);
                    }

                    if (indices.Count >= 3)
                    {
                        // Fan triangulation.
                        for (var i = 1; i < indices.Count - 1; i++)
                        {
                            faces.Add((indices[0], indices[i], indices[i + 1]));
                        }
                    }
                    break;
            }
        }

        if (vertices.Count == 0)
            throw new InvalidDataException("OBJ file contains no vertices");

        var min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
        var max = new double[] { double.MinValue, double.MinValue, double.MinValue };
        foreach (var (x, y, z) in vertices)
        {
            min[0] = Math.Min(min[0], x); max[0] = Math.Max(max[0], x);
            min[1] = Math.Min(min[1], y); max[1] = Math.Max(max[1], y);
            min[2] = Math.Min(min[2], z); max[2] = Math.Max(max[2], z);
        }

        double signedVolume = 0, absVolume = 0;
        foreach (var (a, b, c) in faces)
        {
            var tri = SignedTetraVolume(vertices[a], vertices[b], vertices[c]);
            signedVolume += tri;
            absVolume += Math.Abs(tri);
        }

        var closed = faces.Count > 0 && Math.Abs(signedVolume) > 1e-6;
        var volume = closed ? Math.Abs(signedVolume) : absVolume;

        return new MeshStats(
            WidthMm: max[0] - min[0],
            DepthMm: max[1] - min[1],
            HeightMm: max[2] - min[2],
            VolumeCm3: volume / 1000.0,
            VertexCount: vertices.Count,
            FaceCount: faces.Count);
    }

    private static bool TryParse(string[] tokens, out (double X, double Y, double Z) point)
    {
        point = (0, 0, 0);
        if (!double.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return false;
        }
        point = (x, y, z);
        return true;
    }

    private static int? ResolveIndex(string raw, int vertexCount)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) || idx == 0)
            return null;

        // OBJ indices are 1-based; negative counts back from the current end of the list.
        return idx > 0 ? idx - 1 : vertexCount + idx;
    }

    private static double SignedTetraVolume((double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c)
    {
        var cx = b.Y * c.Z - b.Z * c.Y;
        var cy = b.Z * c.X - b.X * c.Z;
        var cz = b.X * c.Y - b.Y * c.X;
        return (a.X * cx + a.Y * cy + a.Z * cz) / 6.0;
    }
}