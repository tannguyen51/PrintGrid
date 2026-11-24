using System.Numerics;
using PrintGrid.Infrastructure.Shared.Slicing;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

internal static class ThreeMfMeshParser
{
    public static MeshStats Parse(Stream stream)
    {
        // Use actual build items, local vertex indices, units and component transforms.
        // Material boundaries are not holes. Test topology on each original object,
        // before painting subdivides its surface or separates colors.
        var document = ThreeMfPackage.ReadNative(stream, readColors: false);
        var plates = new Dictionary<string, (Vector3 Min, Vector3 Max)>();
        var triangleCount = 0;
        var closed = true; var manifold = true; double volume = 0;
        foreach (var mesh in document.Meshes)
        {
            (Vector3 Min, Vector3 Max) bounds = plates.GetValueOrDefault(mesh.Plate,
                (new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity)));
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
            foreach (var face in mesh.Faces)
            {
                var t = new QuoteMesh.Triangle(mesh.Vertices[face.A], mesh.Vertices[face.B], mesh.Vertices[face.C]);
                triangleCount++;
                bounds.Min = Vector3.Min(bounds.Min, Vector3.Min(t.A, Vector3.Min(t.B, t.C)));
                bounds.Max = Vector3.Max(bounds.Max, Vector3.Max(t.A, Vector3.Max(t.B, t.C)));
                signed += Vector3.Dot(t.A, Vector3.Cross(t.B, t.C)) / 6d;
                Edge(t.A, t.B); Edge(t.B, t.C); Edge(t.C, t.A);
            }
            volume += Math.Abs(signed) / 1000d;
            closed &= edges.Count > 0 && edges.Values.All(n => n == 2);
            manifold &= edges.Values.All(n => n <= 2);
            plates[mesh.Plate] = bounds;
        }
        // Printer feasibility concerns each actual plate, not empty space between
        // plate origins in a multi-plate authoring project.
        return new(plates.Values.Max(p => p.Max.X - p.Min.X), plates.Values.Max(p => p.Max.Y - p.Min.Y),
            plates.Values.Max(p => p.Max.Z - p.Min.Z), volume,
            triangleCount * 3, triangleCount, closed, manifold);
    }
}
