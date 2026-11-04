using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Mesh preparation only; all extrusion/time estimates come from CuraEngine.</summary>
public static class QuoteMesh
{
    public sealed record Triangle(Vector3 A, Vector3 B, Vector3 C);

    public static IReadOnlyList<List<Triangle>> Read(Stream stream, string format)
    {
        try { return ReadCore(stream, format); }
        catch (Exception e) when (e is XmlException or FormatException or OverflowException or ArgumentException or IndexOutOfRangeException or NullReferenceException)
        { throw new InvalidDataException("File mesh thiếu dữ liệu, tọa độ hoặc tham chiếu không hợp lệ.", e); }
    }

    private static IReadOnlyList<List<Triangle>> ReadCore(Stream stream, string format)
    {
        var groups = new SortedDictionary<string, List<Triangle>>(StringComparer.Ordinal);
        if (format.TrimStart('.').Equals("3MF", StringComparison.OrdinalIgnoreCase)) Read3Mf(stream, groups);
        else if (format.TrimStart('.').Equals("STL", StringComparison.OrdinalIgnoreCase)) groups["0"] = ReadStl(stream);
        else throw new InvalidDataException("Báo giá CuraEngine hiện nhận STL một màu hoặc 3MF có phân vùng vật liệu. Hãy xuất file sang định dạng phù hợp.");
        if (groups.Count == 0 || groups.Values.Any(g => g.Count == 0) || groups.Values.Sum(g => (long)g.Count) > 1_000_000)
            throw new InvalidDataException("File rỗng hoặc vượt giới hạn 1 triệu mặt cho báo giá tự động.");
        return groups.Values.ToList();
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
                if (count > 1_000_000) throw new InvalidDataException("File quá nhiều mặt.");
                var triangles = new List<Triangle>();
                for (var i = 0; i < count; i++)
                {
                    reader.ReadBytes(12);
                    Vector3 Point() => Checked(new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                    triangles.Add(new(Point(), Point(), Point()));
                    reader.ReadUInt16();
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
            if (tokens.Length != 4 || tokens[0] != "vertex") continue;
            vertices.Add(Checked(new(F(tokens[1]), F(tokens[2]), F(tokens[3]))));
            if (vertices.Count > 3_000_000) throw new InvalidDataException("File quá nhiều mặt.");
        }
        if (vertices.Count == 0 || vertices.Count % 3 != 0) throw new InvalidDataException("STL không hợp lệ.");
        return vertices.Chunk(3).Select(v => new Triangle(v[0], v[1], v[2])).ToList();
    }

    private static void Read3Mf(Stream stream, SortedDictionary<string, List<Triangle>> groups)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("3D/3dmodel.model") ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase));
        if (entry is null || entry.Length > 100_000_000) throw new InvalidDataException("3MF thiếu model hoặc quá lớn.");
        using var input = entry.Open();
        using var xml = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 100_000_000 });
        var doc = XDocument.Load(xml);
        var root = doc.Root ?? throw new InvalidDataException("3MF rỗng.");
        var ns = root.Name.Namespace;
        var unit = (string?)root.Attribute("unit") ?? "millimeter";
        var factor = unit switch { "millimeter" => 1f, "micron" => .001f, "centimeter" => 10f,
            "meter" => 1000f, "inch" => 25.4f, "foot" => 304.8f, _ => throw new InvalidDataException("Đơn vị 3MF không hỗ trợ.") };
        var objects = root.Element(ns + "resources")?.Elements(ns + "object").ToDictionary(o => (string)o.Attribute("id")!)
            ?? throw new InvalidDataException("3MF thiếu resources.");
        var total = 0;
        void Visit(string id, Matrix4x4 transform, HashSet<string> path)
        {
            if (!path.Add(id) || path.Count > 64 || !objects.TryGetValue(id, out var obj)) throw new InvalidDataException("3MF tham chiếu component không hợp lệ.");
            var mesh = obj.Element(ns + "mesh");
            if (mesh is not null)
            {
                var vertices = mesh.Element(ns + "vertices")!.Elements(ns + "vertex")
                    .Select(v => Checked(Vector3.Transform(new(F((string)v.Attribute("x")!), F((string)v.Attribute("y")!), F((string)v.Attribute("z")!)), transform) * factor)).ToArray();
                foreach (var t in mesh.Element(ns + "triangles")!.Elements(ns + "triangle"))
                {
                    var p = (string?)t.Attribute("p1") ?? (string?)obj.Attribute("pindex") ?? "0";
                    if (((string?)t.Attribute("p2") is { } p2 && p2 != p) || ((string?)t.Attribute("p3") is { } p3 && p3 != p))
                        throw new InvalidDataException("3MF có màu nội suy trên mặt. Hãy xuất các vùng màu thành vật liệu riêng.");
                    var pid = (string?)t.Attribute("pid") ?? (string?)obj.Attribute("pid") ?? "0";
                    var key = $"{pid.PadLeft(10, '0')}:{p.PadLeft(10, '0')}";
                    if (!groups.TryGetValue(key, out var list)) groups[key] = list = new();
                    int Index(string a) => int.Parse((string)t.Attribute(a)!, CultureInfo.InvariantCulture);
                    list.Add(new(vertices[Index("v1")], vertices[Index("v2")], vertices[Index("v3")]));
                    if (++total > 1_000_000) throw new InvalidDataException("File quá nhiều mặt.");
                }
            }
            foreach (var c in obj.Element(ns + "components")?.Elements(ns + "component") ?? [])
            {
                if (c.Attributes().Any(a => a.Name.LocalName == "path")) throw new InvalidDataException("3MF tham chiếu model ngoài chưa được hỗ trợ.");
                Visit((string)c.Attribute("objectid")!, Transform((string?)c.Attribute("transform")) * transform, new(path));
            }
        }
        foreach (var item in root.Element(ns + "build")?.Elements(ns + "item") ?? [])
            Visit((string)item.Attribute("objectid")!, Transform((string?)item.Attribute("transform")), new());
    }

    private static Matrix4x4 Transform(string? raw)
    {
        if (raw is null) return Matrix4x4.Identity;
        var v = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(F).ToArray();
        if (v.Length != 12) throw new InvalidDataException("Transform 3MF không hợp lệ.");
        return new(v[0], v[1], v[2], 0, v[3], v[4], v[5], 0, v[6], v[7], v[8], 0, v[9], v[10], v[11], 1);
    }
    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
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
