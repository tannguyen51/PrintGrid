using System.Numerics;
using PrintGrid.Infrastructure.Shared.Slicing;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

internal static class ThreeMfMeshParser
{
    public static MeshStats Parse(Stream stream)
    {
        // Use actual build items, local vertex indices, units and component transforms.
        var groups = QuoteMesh.Read(stream, "3MF");
        var triangles = groups.SelectMany(g => g).ToList();
        var vertices = triangles.SelectMany(t => new[] { t.A, t.B, t.C }).ToList();
        var min = vertices.Aggregate(Vector3.Min); var max = vertices.Aggregate(Vector3.Max);
        var closed = true; var manifold = true; double volume = 0;
        foreach (var group in groups)
        {
            var edges = new Dictionary<(Vector3, Vector3), int>();
            var signed = 0d;
            void Edge(Vector3 a, Vector3 b)
            {
                var order = a.X.CompareTo(b.X);
                if (order == 0) order = a.Y.CompareTo(b.Y);
                if (order == 0) order = a.Z.CompareTo(b.Z);
                var key = order < 0 ? (a, b) : (b, a);
                edges[key] = edges.GetValueOrDefault(key) + 1;
            }
            foreach (var t in group)
            {
                signed += Vector3.Dot(t.A, Vector3.Cross(t.B, t.C)) / 6d;
                Edge(t.A, t.B); Edge(t.B, t.C); Edge(t.C, t.A);
            }
            volume += Math.Abs(signed) / 1000d;
            closed &= edges.Count > 0 && edges.Values.All(n => n == 2);
            manifold &= edges.Values.All(n => n <= 2);
        }
        return new(max.X - min.X, max.Y - min.Y, max.Z - min.Z, volume,
            vertices.Count, triangles.Count, closed, manifold);
    }
}
