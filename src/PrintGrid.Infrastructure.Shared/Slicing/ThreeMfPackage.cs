using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Bounded, streaming 3MF reader. Geometry analysis keeps original faces; color grouping is for slicing.</summary>
public static class ThreeMfPackage
{
    public sealed record ColorGroup(string Id, List<QuoteMesh.Triangle> Triangles, string? OriginalColorHex = null);
    public readonly record struct NativeFace(int A, int B, int C, string? Paint, string GroupId);
    public sealed record NativeMesh(string RootInstance, string Plate, Vector3[] Vertices, NativeFace[] Faces, int DefaultSlot);
    public sealed class NativeDocument
    {
        public List<NativeMesh> Meshes { get; } = [];
        public Dictionary<string, string?> Palette { get; } = new(StringComparer.Ordinal);
        public HashSet<string> GroupIds { get; } = new(StringComparer.Ordinal);
        public bool HasSurfacePainting { get; internal set; }
        public int PlateCount => Meshes.Select(m => m.Plate).Distinct().Count();
    }
    public const long MaxExpandedBytes = 512L * 1024 * 1024;
    private const long MaxEntryBytes = 256L * 1024 * 1024;
    private sealed record Face(int A, int B, int C, string? Paint, string? Pid, string? P1, string? P2, string? P3);
    private sealed record Reference(string Id, string? Path, Matrix4x4 Transform);
    private sealed class Part(string id, string? pid, string? index)
    {
        public string Id { get; } = id;
        public string? Pid { get; } = pid;
        public string? Index { get; } = index;
        public List<Vector3> Vertices { get; } = [];
        public List<Face> Faces { get; } = [];
        public List<Reference> Components { get; } = [];
    }
    private sealed class Model
    {
        public float Unit { get; set; } = 1;
        public Dictionary<string, Part> Parts { get; } = [];
        public List<Reference> Build { get; } = [];
    }

    public static IReadOnlyList<List<QuoteMesh.Triangle>> Read(Stream stream, bool splitByColor = true, bool allowSurfacePainting = true)
        => ReadGroups(stream, splitByColor, allowSurfacePainting).Select(g => g.Triangles).ToList();

    public static IReadOnlyList<ColorGroup> ReadGroups(Stream stream, bool splitByColor = true, bool allowSurfacePainting = true)
    {
        try { return ReadCore(stream, splitByColor, allowSurfacePainting); }
        catch (Exception e) when (e is XmlException or FormatException or OverflowException or ArgumentException
            or IndexOutOfRangeException or KeyNotFoundException or JsonException)
        { throw new InvalidDataException("File 3MF thiếu dữ liệu, tọa độ hoặc tham chiếu không hợp lệ.", e); }
    }

    public static NativeDocument ReadNative(Stream stream, bool readColors = true)
    {
        var document = new NativeDocument();
        try { ReadCore(stream, readColors, true, document); return document; }
        catch (Exception e) when (e is XmlException or FormatException or OverflowException or ArgumentException
            or IndexOutOfRangeException or KeyNotFoundException or JsonException)
        { throw new InvalidDataException("File 3MF thiếu dữ liệu, tọa độ hoặc tham chiếu không hợp lệ.", e); }
    }

    private static IReadOnlyList<ColorGroup> ReadCore(Stream stream, bool splitByColor, bool allowSurfacePainting, NativeDocument? native = null)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > 4096 || zip.Entries.Sum(e => e.Length) > MaxExpandedBytes)
            throw new InvalidDataException("3MF vượt giới hạn dữ liệu giải nén 512 MiB.");
        var entries = zip.Entries.ToDictionary(e => Resolve("", e.FullName), StringComparer.Ordinal);
        var models = new Dictionary<string, Model>(StringComparer.Ordinal);
        var objectSlots = new Dictionary<string, int>();
        var partSlots = new Dictionary<(string, string), int>();
        var ignoredParts = new HashSet<(string, string)>();
        var plateInstances = new Dictionary<(string, int), string>();
        var paletteCount = 0;
        string?[] palette = [];
        if (entries.TryGetValue("Metadata/project_settings.config", out var settings))
        {
            using var source = SmallEntry(settings);
            using var json = JsonDocument.Parse(source);
            if (json.RootElement.TryGetProperty("filament_colour", out var colors))
            {
                palette = colors.EnumerateArray().Select(c => c.GetString()).ToArray();
                paletteCount = palette.Length;
                for (var i = 0; i < palette.Length; i++) native?.Palette.TryAdd($"filament:{i + 1:D8}", palette[i]);
            }
        }
        if (entries.TryGetValue("Metadata/model_settings.config", out var metadata))
        {
            using var source = SmallEntry(metadata);
            using var reader = XmlReader.Create(source, XmlSettings(4 * 1024 * 1024));
            var config = XDocument.Load(reader);
            foreach (var obj in config.Root?.Elements("object") ?? [])
            {
                var id = (string?)obj.Attribute("id") ?? "";
                var slot = Slot(obj);
                if (slot is > 0) objectSlots[id] = slot.Value;
                foreach (var part in obj.Elements("part"))
                {
                    var partId = (string?)part.Attribute("id") ?? "";
                    if ((string?)part.Attribute("subtype") is { } subtype && subtype != "normal_part")
                        ignoredParts.Add((id, partId));
                    var partSlot = Slot(part);
                    if (partSlot is > 0) partSlots[(id, partId)] = partSlot.Value;
                }
            }
            foreach (var plate in config.Root?.Elements("plate") ?? [])
            {
                var id = Metadata(plate, "plater_id") ?? "1";
                foreach (var item in plate.Elements("model_instance"))
                    if (Metadata(item, "object_id") is { } objectId)
                        plateInstances[(objectId, int.TryParse(Metadata(item, "instance_id"), out var n) ? n : 0)] = id;
            }
        }
        var mainPath = entries.ContainsKey("3D/3dmodel.model") ? "3D/3dmodel.model"
            : entries.Keys.FirstOrDefault(p => p.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException("3MF thiếu model.");
        var main = Load(mainPath);
        var groups = new SortedDictionary<string, List<QuoteMesh.Triangle>>(StringComparer.Ordinal);
        var total = 0;
        var instance = 0;
        var perObjectInstance = new Dictionary<string, int>();
        var colorKeys = new Dictionary<int, string>();
        var paintSlots = new Dictionary<(string, int), int[]>();
        foreach (var item in main.Build)
        {
            var rootSlot = objectSlots.GetValueOrDefault(item.Id, 1);
            var ordinal = perObjectInstance.GetValueOrDefault(item.Id);
            perObjectInstance[item.Id] = ordinal + 1;
            var plate = plateInstances.GetValueOrDefault((item.Id, ordinal), "1");
            Visit(mainPath, item.Id, item.Transform * Matrix4x4.CreateScale(main.Unit), rootSlot,
                item.Id, (++instance).ToString(CultureInfo.InvariantCulture), plate, []);
        }
        if (native is not null)
        {
            if (native.Meshes.Count == 0 || native.PlateCount > 64) throw new InvalidDataException("3MF rỗng hoặc có quá nhiều bàn in.");
            return [];
        }
        if (groups.Count == 0) throw new InvalidDataException("3MF không có hình học có thể in.");
        return groups.Select(g => new ColorGroup(g.Key, g.Value,
            g.Key.StartsWith("filament:", StringComparison.Ordinal) && int.TryParse(g.Key[9..], out var slot)
                && slot > 0 && slot <= palette.Length ? palette[slot - 1] : null)).ToList();

        void Add(string key, QuoteMesh.Triangle triangle)
        {
            if (++total > QuoteMesh.MaxTriangles) throw new InvalidDataException($"File vượt giới hạn {QuoteMesh.MaxTriangles} mặt sau phân vùng.");
            if (!groups.TryGetValue(key, out var group)) groups[key] = group = [];
            group.Add(triangle);
        }
        void Visit(string file, string id, Matrix4x4 transform, int slot, string rootId, string instanceKey, string plate, HashSet<string> path)
        {
            var identity = file + "#" + id;
            if (path.Count >= 64 || !path.Add(identity)) throw new InvalidDataException("3MF tham chiếu component vòng lặp hoặc quá sâu.");
            var model = Load(file);
            if (!model.Parts.TryGetValue(id, out var part)) throw new InvalidDataException("3MF tham chiếu object không tồn tại.");
            if (ignoredParts.Contains((rootId, id))) return;
            slot = partSlots.GetValueOrDefault((rootId, id), slot);
            if (native is not null && part.Faces.Count > 0)
            {
                var vertices = part.Vertices.Select(v => Checked(Vector3.Transform(v, transform))).ToArray();
                var faces = new NativeFace[part.Faces.Count];
                for (var i = 0; i < part.Faces.Count; i++)
                {
                    var face = part.Faces[i];
                    if (++total > QuoteMesh.MaxTriangles || face.A < 0 || face.B < 0 || face.C < 0
                        || face.A >= vertices.Length || face.B >= vertices.Length || face.C >= vertices.Length)
                        throw new InvalidDataException("3MF vượt giới hạn mặt hoặc tham chiếu đỉnh không tồn tại.");
                    var key = splitByColor ? FaceKey(face) : "";
                    faces[i] = new(face.A, face.B, face.C, splitByColor ? face.Paint : null, key);
                    if (!splitByColor) continue;
                    if (string.IsNullOrEmpty(face.Paint)) native.GroupIds.Add(key);
                    else
                    {
                        native.HasSurfacePainting = true;
                        if (!paintSlots.TryGetValue((face.Paint, slot), out var used))
                        {
                            var states = new HashSet<int>();
                            ThreeMfPaint.Remap(face.Paint, slot, s => { states.Add(s); return 1; });
                            used = [.. states];
                            if (paintSlots.Count < 8192) paintSlots[(face.Paint, slot)] = used;
                        }
                        foreach (var state in used) native.GroupIds.Add(ColorKey(state));
                    }
                }
                native.Meshes.Add(new(instanceKey.Split('/')[0], plate, vertices, faces, slot));
            }
            Vector3 Point(int index)
            {
                if (index < 0 || index >= part.Vertices.Count) throw new InvalidDataException("3MF tham chiếu đỉnh không tồn tại.");
                return Checked(Vector3.Transform(part.Vertices[index], transform));
            }
            foreach (var face in native is null ? part.Faces : [])
            {
                var triangle = new QuoteMesh.Triangle(Point(face.A), Point(face.B), Point(face.C));
                if (!splitByColor) { Add(instanceKey + ":" + identity, triangle); continue; }
                if (!string.IsNullOrEmpty(face.Paint))
                {
                    if (!allowSurfacePainting)
                        throw new InvalidDataException("File có màu tô trên bề mặt. Có thể xem và duyệt model; báo giá in nhiều màu cần slicer hỗ trợ kiểu tô màu này hoặc file có các khối vật liệu riêng.");
                    ThreeMfPaint.Decode(face.Paint, triangle, slot, (t, s) => Add(ColorKey(s), t));
                }
                else if (face.Pid is not null || part.Pid is not null)
                {
                    var p = face.P1 ?? part.Index ?? "0";
                    if ((face.P2 is not null && face.P2 != p) || (face.P3 is not null && face.P3 != p))
                        throw new InvalidDataException("3MF có màu nội suy trên mặt. Hãy xuất các vùng màu thành vật liệu riêng.");
                    Add($"material:{file}:{(face.Pid ?? part.Pid)!.PadLeft(10, '0')}:{p.PadLeft(10, '0')}", triangle);
                }
                else Add(ColorKey(slot), triangle);
            }
            for (var i = 0; i < part.Components.Count; i++)
            {
                var component = part.Components[i];
                var target = component.Path is null ? file : Resolve(file, component.Path);
                var targetModel = Load(target);
                // Component translations use the parent document's units.
                var childTransform = Matrix4x4.CreateScale(targetModel.Unit / model.Unit) * component.Transform * transform;
                Visit(target, component.Id, childTransform, slot, rootId, instanceKey + "/" + i, plate, new(path));
            }
            string FaceKey(Face face)
            {
                if (face.Pid is null && part.Pid is null) return ColorKey(slot);
                var p = face.P1 ?? part.Index ?? "0";
                if ((face.P2 is not null && face.P2 != p) || (face.P3 is not null && face.P3 != p))
                    throw new InvalidDataException("3MF có màu nội suy trên mặt chưa được hỗ trợ cho in sợi.");
                return $"material:{file}:{(face.Pid ?? part.Pid)!.PadLeft(10, '0')}:{p.PadLeft(10, '0')}";
            }
        }
        string ColorKey(int slot)
        {
            if (slot < 1 || slot > 256 || (paletteCount > 0 && slot > paletteCount))
                throw new InvalidDataException("Vùng màu tham chiếu filament không tồn tại.");
            if (!colorKeys.TryGetValue(slot, out var key)) colorKeys[slot] = key = $"filament:{slot:D8}";
            return key;
        }
        Model Load(string name)
        {
            if (models.TryGetValue(name, out var cached)) return cached;
            if (!entries.TryGetValue(name, out var entry) || entry.Length > MaxEntryBytes)
                throw new InvalidDataException("3MF thiếu tệp hình học hoặc tệp vượt 256 MiB.");
            using var source = entry.Open();
            using var xml = XmlReader.Create(source, XmlSettings(MaxEntryBytes));
            var model = new Model();
            Part? part = null;
            var sourceFaces = 0;
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "object") { part = null; continue; }
                if (xml.NodeType != XmlNodeType.Element) continue;
                switch (xml.LocalName)
                {
                    case "model": model.Unit = Unit(xml.GetAttribute("unit")); break;
                    case "object":
                        part = new Part(Required(xml, "id"), xml.GetAttribute("pid"), xml.GetAttribute("pindex"));
                        if (!model.Parts.TryAdd(part.Id, part)) throw new InvalidDataException("3MF có object trùng ID.");
                        break;
                    case "vertex" when part is not null:
                        if (part.Vertices.Count >= QuoteMesh.MaxTriangles * 3) throw new InvalidDataException("3MF có quá nhiều đỉnh.");
                        part.Vertices.Add(Checked(new(Number(xml, "x"), Number(xml, "y"), Number(xml, "z")))); break;
                    case "triangle" when part is not null:
                        if (++sourceFaces > QuoteMesh.MaxTriangles) throw new InvalidDataException("3MF có quá nhiều mặt.");
                        part.Faces.Add(new(Integer(xml, "v1"), Integer(xml, "v2"), Integer(xml, "v3"),
                            xml.GetAttribute("paint_color") ?? xml.GetAttribute("mmu_segmentation", "http://schemas.slic3r.org/3mf/2017/06"),
                            xml.GetAttribute("pid"), xml.GetAttribute("p1"), xml.GetAttribute("p2"), xml.GetAttribute("p3"))); break;
                    case "component" when part is not null:
                        var target = xml.GetAttribute("path", "http://schemas.microsoft.com/3dmanufacturing/production/2015/06");
                        part.Components.Add(new(Required(xml, "objectid"), target, Transform(xml.GetAttribute("transform")))); break;
                    case "item":
                        if (xml.GetAttribute("printable") is not ("0" or "false"))
                            model.Build.Add(new(Required(xml, "objectid"), null, Transform(xml.GetAttribute("transform")))); break;
                }
            }
            models.Add(name, model);
            return model;
        }
    }

    private static Stream SmallEntry(ZipArchiveEntry entry) => entry.Length <= 4 * 1024 * 1024
        ? entry.Open() : throw new InvalidDataException("Cấu hình 3MF quá lớn.");
    private static XmlReaderSettings XmlSettings(long max) => new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = max };
    private static int? Slot(XElement element) => int.TryParse(element.Elements("metadata").FirstOrDefault(e => (string?)e.Attribute("key") == "extruder")?.Attribute("value")?.Value, out var slot) ? slot : null;
    private static string? Metadata(XElement element, string key) => (string?)element.Elements("metadata").FirstOrDefault(e => (string?)e.Attribute("key") == key)?.Attribute("value");
    private static string Required(XmlReader xml, string name) => xml.GetAttribute(name) ?? throw new InvalidDataException($"3MF thiếu {name}.");
    private static int Integer(XmlReader xml, string name) => int.Parse(Required(xml, name), CultureInfo.InvariantCulture);
    private static float Number(XmlReader xml, string name) => float.Parse(Required(xml, name), CultureInfo.InvariantCulture);
    private static float Unit(string? unit) => unit switch { null or "millimeter" => 1, "micron" => .001f, "centimeter" => 10, "meter" => 1000, "inch" => 25.4f, "foot" => 304.8f, _ => throw new InvalidDataException("Đơn vị 3MF không hỗ trợ.") };
    private static Vector3 Checked(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) ? v : throw new InvalidDataException("Tọa độ 3MF không hợp lệ.");
    private static Matrix4x4 Transform(string? raw)
    {
        if (raw is null) return Matrix4x4.Identity;
        var v = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        if (v.Length != 12 || v.Any(n => !float.IsFinite(n))) throw new InvalidDataException("Transform 3MF không hợp lệ.");
        return new(v[0], v[1], v[2], 0, v[3], v[4], v[5], 0, v[6], v[7], v[8], 0, v[9], v[10], v[11], 1);
    }
    private static string Resolve(string source, string target)
    {
        target = Uri.UnescapeDataString(target.Replace('\\', '/'));
        if (target.Contains(':') || target.Contains('?') || target.Contains('#')) throw new InvalidDataException("3MF chỉ được tham chiếu tệp trong gói.");
        var path = target.StartsWith('/') ? target[1..] : (source.Contains('/') ? source[..(source.LastIndexOf('/') + 1)] : "") + target;
        var segments = new List<string>();
        foreach (var segment in path.Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == "..") { if (segments.Count == 0) throw new InvalidDataException("Đường dẫn 3MF vượt ra ngoài gói."); segments.RemoveAt(segments.Count - 1); }
            else segments.Add(segment);
        }
        return string.Join('/', segments);
    }
}
