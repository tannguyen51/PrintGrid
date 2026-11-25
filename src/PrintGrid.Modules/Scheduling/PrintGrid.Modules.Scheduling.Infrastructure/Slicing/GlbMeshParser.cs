using System.Numerics;
using PrintGrid.Infrastructure.Shared.Slicing;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

internal static class GlbMeshParser
{
    public static MeshStats Parse(Stream stream)
    {
        // Analysis and quotation share world-space geometry, including transforms and skinning.
        var triangles = GlbSceneMesh.Read(stream);
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        var vertices = new HashSet<Vector3>();
        var edges = new Dictionary<(Vector3, Vector3), int>();
        double signedVolume = 0, absoluteVolume = 0;
        foreach (var t in triangles)
        {
            min = Vector3.Min(min, Vector3.Min(t.A, Vector3.Min(t.B, t.C)));
            max = Vector3.Max(max, Vector3.Max(t.A, Vector3.Max(t.B, t.C)));
            vertices.Add(t.A); vertices.Add(t.B); vertices.Add(t.C);
            var volume = ((double)t.A.X * ((double)t.B.Y * t.C.Z - (double)t.B.Z * t.C.Y)
                - (double)t.A.Y * ((double)t.B.X * t.C.Z - (double)t.B.Z * t.C.X)
                + (double)t.A.Z * ((double)t.B.X * t.C.Y - (double)t.B.Y * t.C.X)) / 6;
            signedVolume += volume;
            absoluteVolume += Math.Abs(volume);
            Edge(t.A, t.B); Edge(t.B, t.C); Edge(t.C, t.A);
        }
        var size = max - min;
        return new(size.X, size.Y, size.Z,
            (Math.Abs(signedVolume) > 1e-6 ? Math.Abs(signedVolume) : absoluteVolume) / 1000,
            vertices.Count, triangles.Count,
            edges.Count > 0 && edges.Values.All(n => n == 2),
            edges.Values.All(n => n <= 2));

        void Edge(Vector3 a, Vector3 b)
        {
            // Weld exporter seam vertices to 0.1 micrometre, as the previous parser did.
            a = Rounded(a); b = Rounded(b);
            var order = a.X.CompareTo(b.X);
            if (order == 0) order = a.Y.CompareTo(b.Y);
            if (order == 0) order = a.Z.CompareTo(b.Z);
            var key = order < 0 ? (a, b) : (b, a);
            edges[key] = edges.GetValueOrDefault(key) + 1;
        }
    }

    private static Vector3 Rounded(Vector3 p) => new((float)Math.Round(p.X, 4), (float)Math.Round(p.Y, 4), (float)Math.Round(p.Z, 4));
}
