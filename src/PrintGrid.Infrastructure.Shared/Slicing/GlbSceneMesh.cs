using System.Numerics;
using System.Text.Json.Nodes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Flattens the default GLB scene in its authored pose, including node instances and skinning.</summary>
public static class GlbSceneMesh
{
    public static List<QuoteMesh.Triangle> Read(Stream stream)
    {
        try
        {
            // Stream-only loading cannot resolve external paths or network resources.
            var model = ModelRoot.ReadGLB(new BorrowedStream(stream), new ReadSettings { JsonPreprocessor = StaticScene });
            var scene = model.DefaultScene ?? model.LogicalScenes.FirstOrDefault()
                ?? throw new InvalidDataException("File GLB không có cảnh để in.");
            if (model.LogicalNodes.Count > 4096 || model.LogicalMeshes.Count > 4096)
                throw new InvalidDataException("File GLB vượt giới hạn số đối tượng.");

            long count = 0;
            foreach (var node in Node.Flatten(scene))
            {
                if (node.Mesh is null) continue;
                foreach (var primitive in node.Mesh.Primitives)
                foreach (var _ in primitive.GetTriangleIndices())
                    if (++count > QuoteMesh.MaxTriangles) throw TriangleLimit();
            }
            if (count == 0) throw new InvalidDataException("File GLB không có bề mặt tam giác để in.");

            var triangles = new List<QuoteMesh.Triangle>();
            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            // No animation is selected: match the initial pose shown by the model viewer.
            // Evaluation applies parent transforms, inverse bind matrices and morph weights.
            foreach (var triangle in scene.EvaluateTriangles())
            {
                if (triangles.Count >= QuoteMesh.MaxTriangles) throw TriangleLimit();
                var a = Checked(triangle.A.GetGeometry().GetPosition());
                var b = Checked(triangle.B.GetGeometry().GetPosition());
                var c = Checked(triangle.C.GetGeometry().GetPosition());
                min = Vector3.Min(min, Vector3.Min(a, Vector3.Min(b, c)));
                max = Vector3.Max(max, Vector3.Max(a, Vector3.Max(b, c)));
                triangles.Add(new(a, b, c));
            }
            if (triangles.Count == 0) throw new InvalidDataException("File GLB không có bề mặt tam giác để in.");

            // Preserve the existing importer convention for metre-authored small models.
            // Analysis and quotation use the same world-space bounds and units.
            var size = max - min;
            var longest = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (longest is > 0 and < 1)
                for (var i = 0; i < triangles.Count; i++)
                {
                    var t = triangles[i];
                    triangles[i] = new(t.A * 1000, t.B * 1000, t.C * 1000);
                }
            return triangles;
        }
        catch (Exception error) when (error is ModelException or ArgumentException or InvalidOperationException
            or IndexOutOfRangeException or KeyNotFoundException or NotSupportedException or FormatException
            or OverflowException or System.Text.Json.JsonException)
        {
            throw new InvalidDataException("File GLB có dữ liệu glTF, khung xương hoặc định dạng nén không được hỗ trợ. Hãy xuất mesh tĩnh sang GLB, STL hoặc 3MF.", error);
        }
    }

    private static InvalidDataException TriangleLimit() => new($"File vượt giới hạn {QuoteMesh.MaxTriangles} mặt cho báo giá tự động.");
    private static string StaticScene(string json)
    {
        // Animation channels are irrelevant to the authored static pose. Some exporters
        // write invalid animation strides even though their mesh and skin data are valid.
        var root = JsonNode.Parse(json)!.AsObject();
        root.Remove("animations");
        return root.ToJsonString();
    }
    private static Vector3 Checked(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z)
        ? p : throw new InvalidDataException("Tọa độ file GLB không hợp lệ.");

    // SharpGLTF disposes its reader and stream; ownership remains with our caller.
    private sealed class BorrowedStream(Stream source) : Stream
    {
        public override bool CanRead => source.CanRead;
        public override bool CanSeek => source.CanSeek;
        public override bool CanWrite => false;
        public override long Length => source.Length;
        public override long Position { get => source.Position; set => source.Position = value; }
        public override int Read(byte[] buffer, int offset, int count) => source.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => source.Read(buffer);
        public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
