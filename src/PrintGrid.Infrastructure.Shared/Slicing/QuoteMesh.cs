using System.Globalization;
using System.Numerics;
using System.Xml;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Mesh preparation only; all extrusion/time estimates come from CuraEngine.</summary>
public static class QuoteMesh
{
    // Binary STL stores an 84-byte header and 50 bytes per triangle. Support the
    // full 100 MiB local upload limit while retaining a bounded mesh workload.
    public const int MaxTriangles = (100 * 1024 * 1024 - 84) / 50;

    public sealed record Triangle(Vector3 A, Vector3 B, Vector3 C);

    public static IReadOnlyList<List<Triangle>> Read(Stream stream, string format, bool allowSurfacePainting = true)
    {
        try { return ReadCore(stream, format, allowSurfacePainting); }
        catch (Exception e) when (e is XmlException or FormatException or OverflowException or ArgumentException or IndexOutOfRangeException or NullReferenceException)
        { throw new InvalidDataException("File mesh thiếu dữ liệu, tọa độ hoặc tham chiếu không hợp lệ.", e); }
    }

    private static IReadOnlyList<List<Triangle>> ReadCore(Stream stream, string format, bool allowSurfacePainting)
    {
        var groups = new SortedDictionary<string, List<Triangle>>(StringComparer.Ordinal);
        if (format.TrimStart('.').Equals("3MF", StringComparison.OrdinalIgnoreCase)) return ThreeMfPackage.Read(stream, allowSurfacePainting: allowSurfacePainting);
        else if (format.TrimStart('.').Equals("STL", StringComparison.OrdinalIgnoreCase)) groups["0"] = ReadStl(stream);
        else if (format.TrimStart('.').Equals("OBJ", StringComparison.OrdinalIgnoreCase)) groups["0"] = ReadObj(stream);
        else if (format.TrimStart('.').Equals("GLB", StringComparison.OrdinalIgnoreCase)) groups["0"] = GlbSceneMesh.Read(stream);
        else throw new InvalidDataException("Báo giá hỗ trợ STL, OBJ, GLB một màu hoặc 3MF có phân vùng vật liệu.");
        if (groups.Count == 0 || groups.Values.Any(g => g.Count == 0) || groups.Values.Sum(g => (long)g.Count) > MaxTriangles)
            throw new InvalidDataException($"File rỗng hoặc vượt giới hạn {MaxTriangles} mặt cho báo giá tự động.");
        return [.. groups.Values];
    }

    private static List<Triangle> ReadStl(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (stream.Length >= 84)
        {
            stream.Position = 80;
            var count = reader.ReadUInt32();
            if (84L + 50L * count == stream.Length)
            {
                if (count > MaxTriangles) throw new InvalidDataException($"File vượt giới hạn {MaxTriangles} mặt.");
                var triangles = new List<Triangle>();
                for (var i = 0; i < count; i++)
                {
                    reader.ReadBytes(12);
                    triangles.Add(new Triangle(Point(), Point(), Point()));
                    reader.ReadUInt16();
                    continue;
                    Vector3 Point() => Checked(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                }
                return triangles;
            }
        }
        stream.Position = 0;
        using var text = new StreamReader(stream, leaveOpen: true);
        var vertices = new List<Vector3>();
        while (text.ReadLine() is { } line)
        {
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens is not ["vertex", _, _, _]) continue;
            vertices.Add(Checked(new Vector3(F(tokens[1]), F(tokens[2]), F(tokens[3]))));
            if (vertices.Count > 3 * MaxTriangles) throw new InvalidDataException($"File vượt giới hạn {MaxTriangles} mặt.");
        }
        if (vertices.Count == 0 || vertices.Count % 3 != 0) throw new InvalidDataException("STL không hợp lệ.");
        return [.. vertices.Chunk(3).Select(v => new Triangle(v[0], v[1], v[2]))];
    }

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private static List<Triangle> ReadObj(Stream stream)
    {
        using var text = new StreamReader(stream, leaveOpen: true);
        var vertices = new List<Vector3>();
        var triangles = new List<Triangle>();
        while (text.ReadLine() is { } line)
        {
            var tokens = line.Split('#', 2)[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;
            if (tokens[0] == "v")
            {
                if (tokens.Length < 4) throw new InvalidDataException("OBJ thiếu tọa độ đỉnh.");
                if (vertices.Count >= MaxTriangles * 3) throw new InvalidDataException("OBJ vượt giới hạn số đỉnh.");
                vertices.Add(Checked(new(F(tokens[1]), F(tokens[2]), F(tokens[3]))));
            }
            else if (tokens[0] == "f")
            {
                if (tokens.Length < 4) throw new InvalidDataException("OBJ có mặt không hợp lệ.");
                var first = Point(tokens[1]);
                var previous = Point(tokens[2]);
                for (var i = 3; i < tokens.Length; i++)
                {
                    if (triangles.Count >= MaxTriangles) throw new InvalidDataException($"File vượt giới hạn {MaxTriangles} mặt.");
                    var next = Point(tokens[i]);
                    triangles.Add(new(first, previous, next));
                    previous = next;
                }
            }
        }
        return triangles;

        Vector3 Point(string token)
        {
            var index = int.Parse(token.Split('/')[0], CultureInfo.InvariantCulture);
            var resolved = index > 0 ? index - 1 : vertices.Count + index;
            if (index == 0 || resolved < 0 || resolved >= vertices.Count)
                throw new InvalidDataException("OBJ tham chiếu đỉnh không tồn tại.");
            return vertices[resolved];
        }
    }
    private static Vector3 Checked(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z)
        ? v : throw new InvalidDataException("Tọa độ file không hợp lệ.");

    public static void WriteStl(string path, IReadOnlyList<Triangle> triangles, float scale, Vector3 offset)
    {
        using var file = File.Create(path);
        using var writer = new BinaryWriter(file);
        writer.Write(new byte[80]); writer.Write((uint)triangles.Count);
        foreach (var triangle in triangles)
        {
            writer.Write(new byte[12]);
            foreach (var v in new[] { triangle.A, triangle.B, triangle.C })
            { var p = v * scale + offset; writer.Write(p.X); writer.Write(p.Y); writer.Write(p.Z); }
            writer.Write((ushort)0);
        }
    }
}
