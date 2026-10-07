using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

internal static class ThreeMfMeshParser
{
    public static MeshStats Parse(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.Entries.FirstOrDefault(x =>
            x.FullName.StartsWith("3D/", StringComparison.OrdinalIgnoreCase) &&
            x.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("3MF package does not contain a 3D model part");

        using var modelStream = entry.Open();
        var document = XDocument.Load(modelStream, LoadOptions.None);
        var vertices = document.Descendants().Where(x => x.Name.LocalName == "vertex")
            .Select(x => (
                X: Parse(x, "x"),
                Y: Parse(x, "y"),
                Z: Parse(x, "z")))
            .ToList();
        var triangles = document.Descendants().Where(x => x.Name.LocalName == "triangle")
            .Select(x => (A: ParseIndex(x, "v1"), B: ParseIndex(x, "v2"), C: ParseIndex(x, "v3")))
            .ToList();

        if (vertices.Count == 0 || triangles.Count == 0)
            throw new InvalidDataException("3MF model contains no printable mesh");

        var min = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
        var max = new[] { double.MinValue, double.MinValue, double.MinValue };
        foreach (var vertex in vertices)
        {
            min[0] = Math.Min(min[0], vertex.X); max[0] = Math.Max(max[0], vertex.X);
            min[1] = Math.Min(min[1], vertex.Y); max[1] = Math.Max(max[1], vertex.Y);
            min[2] = Math.Min(min[2], vertex.Z); max[2] = Math.Max(max[2], vertex.Z);
        }

        double signedVolume = 0, absoluteVolume = 0;
        var edges = new Dictionary<(int A, int B), int>();
        foreach (var triangle in triangles)
        {
            if (triangle.A < 0 || triangle.B < 0 || triangle.C < 0 ||
                triangle.A >= vertices.Count || triangle.B >= vertices.Count || triangle.C >= vertices.Count)
                throw new InvalidDataException("3MF triangle references a missing vertex");

            var a = vertices[triangle.A]; var b = vertices[triangle.B]; var c = vertices[triangle.C];
            var volume = (a.X * (b.Y * c.Z - b.Z * c.Y)
                - a.Y * (b.X * c.Z - b.Z * c.X)
                + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            signedVolume += volume; absoluteVolume += Math.Abs(volume);
            CountEdge(edges, triangle.A, triangle.B);
            CountEdge(edges, triangle.B, triangle.C);
            CountEdge(edges, triangle.C, triangle.A);
        }

        var closed = Math.Abs(signedVolume) > 1e-6;
        return new MeshStats(
            max[0] - min[0], max[1] - min[1], max[2] - min[2],
            (closed ? Math.Abs(signedVolume) : absoluteVolume) / 1000.0,
            vertices.Count, triangles.Count,
            edges.Count > 0 && edges.Values.All(x => x == 2),
            edges.Values.All(x => x <= 2));
    }

    private static double Parse(XElement element, string attribute) =>
        double.TryParse(element.Attribute(attribute)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value : throw new InvalidDataException($"Invalid 3MF vertex attribute '{attribute}'");

    private static int ParseIndex(XElement element, string attribute) =>
        int.TryParse(element.Attribute(attribute)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : throw new InvalidDataException($"Invalid 3MF triangle attribute '{attribute}'");

    private static void CountEdge(Dictionary<(int A, int B), int> edges, int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        edges[key] = edges.GetValueOrDefault(key) + 1;
    }
}
